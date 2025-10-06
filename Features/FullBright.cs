using FezEngine.Tools;
using FezGame.Services;
using FEZUG.Features.Console;

namespace FEZUG.Features
{
    internal class FullBright : IFezugCommand
    {
        public string Name => "fullbright";
        public string HelpText => "fullbright <on/off> - enables or disables full ambient light in dark levels.";

        private float cachedDiffuse;
        private float cachedAmbient;
        private bool currentState = false;

        [ServiceDependency]
        public IGameLevelManager LevelManager { private get; set; }

        public FullBright()
        {
            ServiceHelper.InjectServices(this);
            LevelManager.LevelChanged += OnLevelChange;
        }

        public List<string> Autocomplete(string[] args)
        {
            return [.. new[] { "on", "off" }.Where(s => s.StartsWith(args[0]))];
        }

        public bool Execute(string[] args)
        {
            if (args.Length != 1)
            {
                FezugConsole.Print($"Incorrect number of parameters: '{args.Length}'", FezugConsole.OutputType.Warning);
                return false;
            }

            switch (args[0])
            {
                case "on":
                    Enable();
                    FezugConsole.Print($"Full brightness has been enabled.");
                    break;
                case "off":
                    Disable();
                    FezugConsole.Print($"Full brightness has been disabled.");
                    break;
            }

            return true;
        }

        private void OnLevelChange()
        {
            if (currentState)
            {
                Enable(true);
            }
        }

        private void Enable(bool force = false)
        {
            if (currentState && !force)
            {
                return;
            }

            cachedDiffuse = LevelManager.BaseDiffuse;
            cachedAmbient = LevelManager.BaseAmbient;
            LevelManager.BaseDiffuse = 1.0f;
            LevelManager.BaseAmbient = 1.0f;
            currentState = true;
        }

        private void Disable()
        {
            if (!currentState)
            {
                return;
            }

            LevelManager.BaseDiffuse = cachedDiffuse;
            LevelManager.BaseAmbient = cachedAmbient;
            currentState = false;
        }
    }
}
