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
        
        public static float Timescale { get; private set; } = 1.0f;

        [ServiceDependency]
        public ISoundManager SoundManager { private get; set; }
        
        public string Name => "timescale";
        public string HelpText => "timescale <value> - changes game's simulation scale";

        public List<string> Autocomplete(string[] args)
        {
            string timescaleStr = Timescale.ToString("0.000", CultureInfo.InvariantCulture);
            if (timescaleStr.StartsWith(args[0])) return [timescaleStr];
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
            var newFunc = (Func<long> original) =>
            {
                var originalTimestamp = original();
                var elapsedSinceMeasure = originalTimestamp - lastMeasuredRealTimestamp;
                internalTimestamp += (long)(elapsedSinceMeasure * Timescale);
                lastMeasuredRealTimestamp = originalTimestamp;
                return internalTimestamp;
            };
            try
            {
                stopwatchTimestampDetour = new Hook(stopwatchTimestampMethod, newFunc);
            }
            catch (Exception e)
            {
                //TODO the Hook contructor throws an exception if Stopwatch.GetTimestamp is a native extern method
                //stopwatchTimestampDetour = new NativeDetour(stopwatchTimestampMethod, newFunc);
            }
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
