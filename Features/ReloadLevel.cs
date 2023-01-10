using FezEngine.Tools;
using FezGame.Services;
using FEZUG.Features.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FEZUG.Features
{
    internal class ReloadLevel : IConsoleCommand
    {
        public string Name => "reload";
        public string HelpText => "reload - reloads current map. If \"fresh\" flag is set, resets all collectibles as well.";

        public List<string> Autocomplete(string args) => null;

        [ServiceDependency]
        public IGameLevelManager LevelManager { private get; set; }

        [ServiceDependency]
        public IGameStateManager GameState { get; set; }

        public bool Execute(string[] args)
        {
            if (args.Length > 0)
            {
                FEZUG.Console.Print($"Incorrect number of parameters: '{args.Length}'", ConsoleLine.OutputType.Warning);
                return false;
            }

            WarpLevel.Warp(LevelManager.Name);

            FEZUG.Console.Print($"Current level ({LevelManager.Name}) has been reloaded.");

            return true;
        }
    }
}
