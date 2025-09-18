using FezEngine.Tools;
using FezGame.Services;
using FEZUG.Features.Console;

namespace FEZUG.Features
{
    internal class ReloadLevel : IFezugCommand
    {
        public string Name => "reload";
        public string HelpText => "reload - reloads current map. If \"fresh\" flag is set, resets all collectibles as well.";

        public List<string> Autocomplete(string[] args) => null;

        [ServiceDependency]
        public IGameLevelManager LevelManager { private get; set; }

        [ServiceDependency]
        public IGameStateManager GameState { get; set; }

        public bool Execute(string[] args)
        {
            if (args.Length > 0)
            {
                FezugConsole.Print($"Incorrect number of parameters: '{args.Length}'", FezugConsole.OutputType.Warning);
                return false;
            }

            WarpLevel.Warp(LevelManager.Name);

            FezugConsole.Print($"Current level ({LevelManager.Name}) has been reloaded.");

            return true;
        }
    }
}
