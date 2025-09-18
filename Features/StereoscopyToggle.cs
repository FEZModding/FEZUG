using FezEngine.Tools;
using FezGame.Services;
using FEZUG.Features.Console;

namespace FEZUG.Features
{
    internal class StereoscopyToggle : IFezugCommand
    {
        public string Name => "stereoscopy";

        public string HelpText => "stereoscopy <on/off/toggle> - changes the state of stereoscopy mode";


        [ServiceDependency]
        public IGameStateManager GameState { private get; set; }


        public StereoscopyToggle()
        {
            ServiceHelper.InjectServices(this);
        }

        public List<string> Autocomplete(string[] args)
        {
            return [.. new string[] { "on", "off", "toggle" }.Where(s => s.StartsWith(args[0]))];
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
                    GameState.StereoMode = true;
                    FezugConsole.Print($"Stereoscopy mode has been enabled.");
                    break;
                case "off":
                    GameState.StereoMode = false;
                    FezugConsole.Print($"Stereoscopy mode has been disabled.");
                    break;
                case "toggle":
                    GameState.StereoMode = !GameState.StereoMode;
                    FezugConsole.Print($"Stereoscopy mode has been toggled to {(GameState.StereoMode ? "enable" : "disable")}.");
                    break;
                default:
                    FezugConsole.Print($"Unknown parameter: '{args[0]}'", FezugConsole.OutputType.Warning);
                    return false;
            }

            return true;
        }
    }
}
