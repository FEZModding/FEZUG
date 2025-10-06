using FezGame;
using FEZUG.Features.Console;
using Microsoft.Xna.Framework;
using MonoMod.RuntimeDetour;
using System.Globalization;
using System.Reflection;
using Mono.Cecil.Cil;
using MonoMod.Cil;

namespace FEZUG.Features
{
    internal class Timescaler : IFezugCommand, IFezugFeature
    {
        private IDetour gameTickILDetour;

        public static float Timescale { get; private set; } = 1.0f;

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


        public void Initialize()
        {
            var targetMethod = typeof(Game).GetMethod("Tick", BindingFlags.Public | BindingFlags.Instance);
            gameTickILDetour = new ILHook(targetMethod, InjectTickMultiplier);
        }

        void InjectTickMultiplier(ILContext il)
        {
            var cursor = new ILCursor(il);
            cursor.GotoNext(i => i.MatchCall("System.TimeSpan", "FromTicks"));
            cursor.EmitDelegate<Func<long, long>>(ticks => (long)(ticks * Timescale));
        }
        
        public void Update(GameTime gameTime){}

        public void DrawHUD(GameTime gameTime) { }
        public void DrawLevel(GameTime gameTime) { }

        public static void SetTimescale(float timescale)
        {
            Timescale = Math.Min(Math.Max(timescale, 0.0001f), 100.0f);
        }
        
        public static GameTime GetUnscaledGameTime(GameTime originalGameTime)
        {
            return new GameTime(
                originalGameTime.TotalGameTime,
                TimeSpan.FromSeconds(originalGameTime.ElapsedGameTime.TotalSeconds / Timescale)
            );
        }
    }
}
