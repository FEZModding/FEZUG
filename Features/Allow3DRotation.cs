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
    internal class Allow3DRotation : IConsoleCommand
    {
        public string Name => "allow3d";
        public string HelpText => "allow3d - allows the player to use 3d rotation";

        public List<string> Autocomplete(string args) => null;

        [ServiceDependency]
        public IGameStateManager GameState { private get; set; }

        [ServiceDependency]
        public IGameLevelManager LevelManager { private get; set; }

        [ServiceDependency]
        public IPlayerManager PlayerManager { private get; set; }

        public bool Execute(string[] args)
        {
            GameState.DisallowRotation = false;
            LevelManager.Flat = false;
            PlayerManager.CanRotate = true;

            FEZUG.Console.Print("Open your eyes...");

            return true;
        }
    }
}
