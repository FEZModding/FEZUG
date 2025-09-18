using FezEngine.Services;
using FezEngine.Tools;
using FezGame;
using FEZUG.Features.Console;
using Microsoft.Xna.Framework;
using MonoMod.RuntimeDetour;
using System.Globalization;
using System.Reflection;

namespace FEZUG.Features
{
    internal class Timescaler : IFezugCommand, IFezugFeature
    {
        private IDetour mainGameUpdateLoopDetour;
        private IDetour mainGameDrawLoopDetour;
        private double timescaledGameTime;
        private double timescaledElapsedTime;

        public static float Timescale = 1.0f;

        public bool Enabled => Timescale != 1.0f;

        public string Name => "timescale";
        public string HelpText => "timescale <value> - changes game's simulation scale";

        [ServiceDependency]
        public ISoundManager SoundManager { private get; set; }

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

            Timescale = timescale;

            FezugConsole.Print($"Timescale has been set to {Timescale.ToString("0.000", CultureInfo.InvariantCulture)}");

            return true;
        }


        public void Initialize()
        {
            timescaledGameTime = 0.0f;

            mainGameUpdateLoopDetour = new Hook(
                typeof(Fez).GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance),
                delegate (Action<Fez, GameTime> original, Fez self, GameTime gameTime)
                {
                    UpdateHooked(original, self, gameTime);
                }
            );
            mainGameDrawLoopDetour = new Hook(
                typeof(Fez).GetMethod("Draw", BindingFlags.NonPublic | BindingFlags.Instance),
                delegate (Action<Fez, GameTime> original, Fez self, GameTime gameTime)
                {
                    DrawHooked(original, self, gameTime);
                }
            );
        }

        private void UpdateHooked(Action<Fez, GameTime> original, Fez self, GameTime gameTime)
        {
            if (gameTime.TotalGameTime.Ticks == 0)
            {
                timescaledGameTime = 0.0f;
            }
            timescaledElapsedTime = gameTime.ElapsedGameTime.TotalSeconds * Timescaler.Timescale;
            timescaledGameTime += timescaledElapsedTime;

            original(self, new GameTime(
                TimeSpan.FromSeconds(timescaledGameTime),
                TimeSpan.FromSeconds(timescaledElapsedTime)
            ));
        }

        private void DrawHooked(Action<Fez, GameTime> original, Fez self, GameTime gameTime)
        {
            original(self, new GameTime(
                TimeSpan.FromSeconds(timescaledGameTime),
                TimeSpan.FromSeconds(timescaledElapsedTime)
            ));
        }


        public void Update(GameTime gameTime){}

        public void DrawHUD(GameTime gameTime) { }
        public void DrawLevel(GameTime gameTime) { }
    }
}
