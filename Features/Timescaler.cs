using System.Diagnostics;
using FEZUG.Features.Console;
using Microsoft.Xna.Framework;
using MonoMod.RuntimeDetour;
using System.Globalization;
using System.Reflection;
using FezEngine.Services;
using FezEngine.Tools;
using Microsoft.Xna.Framework.Audio;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System.Runtime.InteropServices;

namespace FEZUG.Features
{
    internal class Timescaler : IFezugCommand, IFezugFeature
    {
        private IDetour stopwatchTimestampDetour;
        private IDetour setSourcePitchDetour;
        private List<SoundEffectInstance> soundInstancePoolRef;
        private List<DynamicSoundEffectInstance> dynamicSoundInstancePoolRef;

        private static long lastMeasuredRealTimestamp = 0;
        private static long internalTimestamp = 0;
        private static readonly object timerLock = new object();
        delegate long GetTimestampDelegate();
        private static GetTimestampDelegate orig_GetTimestamp;
        // Store the delegate and a raw pointer handle to pin it in unmanaged RAM
        private static GetTimestampDelegate pinnedNativeHook;
        private static GCHandle pinnedHandle, pinnedHandle2, pinnedHandle3, pinnedHandle4;

        public static float Timescale { get; private set; } = 1.0f;

        [ServiceDependency]
        public ISoundManager SoundManager { private get; set; }

        public string Name => "timescale";
        public string HelpText => "timescale <value> - changes game's simulation scale";

        public List<string> Autocomplete(string[] args)
        {
            string timescaleStr = Timescale.ToString("0.000", CultureInfo.InvariantCulture);
            if (timescaleStr.StartsWith(args[0])) return new() {timescaleStr};
            return null;
        }

        public bool Execute(string[] args)
        {
            if (args.Length != 1)
            {
                FezugConsole.Print($"Incorrect number of parameters: '{args.Length}'", FezugConsole.OutputType.Warning);
                return false;
            }

            if (!float.TryParse(args[0], NumberStyles.Number, CultureInfo.InvariantCulture, out float timescale))
            {
                FezugConsole.Print($"Incorrect timescale: '{args[0]}'", FezugConsole.OutputType.Warning);
                return false;
            }

            SetTimescale(timescale);

            FezugConsole.Print($"Timescale has been set to {Timescale.ToString("0.000", CultureInfo.InvariantCulture)}");

            return true;
        }

        private void SetTimescale(float timescale)
        {
            Timescale = Math.Min(Math.Max(timescale, 0.0001f), 100.0f);
            RefreshSoundPitches();
        }


        public void Initialize()
        {
            InjectStopwatch();
            InitializeAudioHooksAndReferences();
        }

        private void InjectStopwatch()
        {
            // TODO?: This could've been done less invasively by manually handling all cases where Stopwatch is used
            // for timing in the game, but they are quite annoying (like in ActiveTrackedSong).

            var stopwatchTimestampMethod = Stopwatch.GetTimestamp;

            try
            {
                // try the managed hook first because Windows doesn't like it when applications directly modify the system code
                // (i.e., other Windows applications use timestamps as well, and a native detour will break those, causing an AccessViolationException)
                stopwatchTimestampDetour = new Hook(stopwatchTimestampMethod, GetTimestampHookManaged);
            }
            catch
            {
                // native/unmanaged code is fragile
                pinnedNativeHook = new GetTimestampDelegate(GetTimestampHookUnmanaged);
                stopwatchTimestampDetour = new NativeDetour(stopwatchTimestampMethod, pinnedNativeHook,
                    new NativeDetourConfig() { ManualApply = true });
                pinnedHandle3 = GCHandle.Alloc(stopwatchTimestampDetour, GCHandleType.Pinned);
                orig_GetTimestamp = stopwatchTimestampDetour.GenerateTrampoline<GetTimestampDelegate>();
                pinnedHandle4 = GCHandle.Alloc(orig_GetTimestamp, GCHandleType.Pinned);
                stopwatchTimestampDetour.Apply();
            }
        }

        private static long GetTimestampHookManaged(Func<long> original)
        {
            // lock to prevent multiple threads from simultaneously updating lastMeasuredRealTimestamp and internalTimestamp
            // also to prevent any other threads from getting the timestamp before we update it
            lock (timerLock)
            {
                return DoAdjustTimestamp(original());
            }
        }
        private static long GetTimestampHookUnmanaged()
        {
            // lock to prevent multiple threads from simultaneously updating lastMeasuredRealTimestamp and internalTimestamp
            // also to prevent any other threads from getting the timestamp before we update it
            lock (timerLock)
            {
                // Absolute fallback safety guard for Wine Mono environments
                long rawTime = (orig_GetTimestamp != null) ? orig_GetTimestamp() : DateTime.UtcNow.Ticks;
                return DoAdjustTimestamp(rawTime);
            }
        }
        // Please ensure lock timerLock is locked before calling this method otherwise page faults can occur
        private static long DoAdjustTimestamp(long originalTimestamp)
        {
            // Note: the reason the lock is not here is because otherwise the other threads could get a timestamp
            //         from the original GetTimestamp method which might mess up the results of this method
            var elapsedSinceMeasure = originalTimestamp - lastMeasuredRealTimestamp;
            internalTimestamp += (long)(elapsedSinceMeasure * Timescale);
            lastMeasuredRealTimestamp = originalTimestamp;
            return internalTimestamp;
        }

        private void InitializeAudioHooksAndReferences()
        {
            var openAlDeviceType = typeof(SoundEffect).Assembly.GetType("Microsoft.Xna.Framework.Audio.OpenALDevice");
            var setSourcePitchMethod = openAlDeviceType.GetMethod("SetSourcePitch", BindingFlags.Public | BindingFlags.Instance);
            setSourcePitchDetour = new ILHook(setSourcePitchMethod, InjectPitchTweak);

            var audioDeviceType = Assembly.GetAssembly(typeof(SoundEffectInstance)).GetType("Microsoft.Xna.Framework.Audio.AudioDevice");
            var soundInstancePoolField = audioDeviceType.GetField("InstancePool", BindingFlags.Public | BindingFlags.Static);
            soundInstancePoolRef = (List<SoundEffectInstance>)soundInstancePoolField!.GetValue(null);
            var dynamicSoundInstancePoolField = audioDeviceType.GetField("DynamicInstancePool", BindingFlags.Public | BindingFlags.Static);
            dynamicSoundInstancePoolRef = (List<DynamicSoundEffectInstance>)dynamicSoundInstancePoolField!.GetValue(null);
        }

        private void InjectPitchTweak(ILContext il)
        {
            var cursor = new ILCursor(il);

            cursor.Emit(OpCodes.Ldarg_2); // load pitch param
            cursor.EmitDelegate<Func<float, float>>(pitch => pitch + (float)Math.Log(Timescale, 2.0));
            cursor.Emit(OpCodes.Starg, 2);

            cursor.Emit(OpCodes.Ldc_I4_0); // false
            cursor.Emit(OpCodes.Starg, 3); // store to clamp argument
        }

        private void RefreshSoundPitches()
        {
            foreach (var audio in EnumerateActiveSoundsForPitchUpdate())
            {
                audio.Pitch = audio.Pitch;
            }
        }

        private IEnumerable<SoundEffectInstance> EnumerateActiveSoundsForPitchUpdate()
        {
            foreach (var sound in soundInstancePoolRef) yield return sound;
            foreach (var sound in dynamicSoundInstancePoolRef) yield return sound;
            foreach (var emitter in SoundManager.Emitters) yield return emitter.Cue;
        }

        public void Update(GameTime gameTime){}

        public void DrawHUD(GameTime gameTime) { }
        public void DrawLevel(GameTime gameTime) { }

        public static GameTime GetUnscaledGameTime(GameTime originalGameTime)
        {
            return new GameTime(
                originalGameTime.TotalGameTime,
                TimeSpan.FromSeconds(originalGameTime.ElapsedGameTime.TotalSeconds / Timescale)
            );
        }
    }
}
