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
    internal class ItemCountSet : IConsoleCommand
    {
        public string Name => "itemcount";
        public string HelpText => "itemcount <goldens/antis/bits/hearts/keys/owls> <count> - sets number of collected items of given type.";

        [ServiceDependency]
        public IGameLevelManager LevelManager { private get; set; }

        [ServiceDependency]
        public IGameStateManager GameState { get; set; }

        public bool Execute(string[] args)
        {
            if (args.Length != 2)
            {
                FEZUG.Console.Print($"Incorrect number of parameters: '{args.Length}'", ConsoleLine.OutputType.Warning);
                return false;
            }

            Int32.TryParse(args[1], out var amount);
            string itemName = "";

            switch (args[0])
            {
                case "goldens":
                    itemName = "golden cubes";
                    GameState.SaveData.CubeShards = amount;
                    break;
                case "antis":
                    itemName = "anti-cubes";
                    GameState.SaveData.SecretCubes = amount;
                    break;
                case "bits":
                    itemName = "cube bits";
                    GameState.SaveData.CollectedParts = amount;
                    break;
                case "hearts":
                    itemName = "heart cubes";
                    GameState.SaveData.PiecesOfHeart = amount;
                    break;
                case "keys":
                    itemName = "keys";
                    GameState.SaveData.Keys = amount;
                    break;
                case "owls":
                    itemName = "owls";
                    GameState.SaveData.CollectedOwls = amount;
                    break;
                default:
                    FEZUG.Console.Print($"Incorrect item name: '{args[0]}'", ConsoleLine.OutputType.Warning);
                    return false;
            }

            FEZUG.Console.Print($"Number of {itemName} has been changed to {amount}.");

            return true;
        }

        public List<string> Autocomplete(string args)
        {
            if (args.Contains(' ')) return null;
            return new string[] { 
                $"goldens {GameState.SaveData.CubeShards}", 
                $"antis {GameState.SaveData.SecretCubes}", 
                $"bits {GameState.SaveData.CollectedParts}",
                $"hearts {GameState.SaveData.PiecesOfHeart}",
                $"keys {GameState.SaveData.Keys}",
                $"owls {GameState.SaveData.CollectedOwls}",
            }.Where(s => s.StartsWith(args)).ToList();
        }
    }
}
