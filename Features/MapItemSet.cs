using FezEngine.Tools;
using FezGame.Services;
using FEZUG.Features.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FEZUG.Features
{
    internal class MapItemSet : IFezugCommand
    {
        public string Name => "maps";

        public string HelpText => "maps <give/remove/list> [name] - gives or removes a map to/from your inventory.";

        [ServiceDependency]
        public IGameStateManager GameState { private get; set; }

        public List<string> GetMapList()
        {
            return MemoryContentManager.AssetNames
                .Where(s => s.ToLower().StartsWith("other textures\\maps\\"))
                .Select(s => Regex.Match(s.ToUpper(), @".*\\(.*?)(?:_\d+.*)").Groups[1].Value)
                .Distinct().ToList();
        }

        public List<string> Autocomplete(string[] args)
        {
            if(args.Length == 1)
            {
                return new string[] { "give", "remove", "list" }
                .Where(s => s.StartsWith(args[0], StringComparison.OrdinalIgnoreCase))
                .ToList();
            }
            if(args.Length == 2)
            {
                return GetMapList()
                    .Select(s=>s.ToLower())
                    .Where(s => s.StartsWith(args[1], StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }
            return null;
        }

        public bool Execute(string[] args)
        {
            if (args.Length >= 1 && args[0] != "give" && args[0] != "remove" && args[0] != "list")
            {
                FezugConsole.Print($"Invalid argument: '{args[0]}'", FezugConsole.OutputType.Warning);
                return false;
            }

            var mapList = GetMapList();

            if (args.Length != 2 || args[0] == "list")
            {
                if(args.Length >= 1 && (args[0] == "remove" || args[0] == "list"))
                {
                    FezugConsole.Print($"List of maps in your inventory:");
                    FezugConsole.Print($"{String.Join(", ", GameState.SaveData.Maps.Select(s=>s.ToLower()))}");
                }
                else
                {
                    FezugConsole.Print($"List of available maps:");
                    FezugConsole.Print($"{String.Join(", ", mapList.Select(s => s.ToLower()))}");
                }
                return true;
            }
            
            var mapName = args[1].ToUpper();

            if (!mapList.Contains(args[1].ToUpper()))
            {
                FezugConsole.Print($"Map with given name does not exist.", FezugConsole.OutputType.Warning);
                return false;
            }

            if(args[0] == "remove")
            {
                if (!GameState.SaveData.Maps.Contains(mapName))
                {
                    FezugConsole.Print("You don't have this map in your inventory.", FezugConsole.OutputType.Warning);
                    return false;
                }
                GameState.SaveData.Maps.Remove(mapName);
                FezugConsole.Print($"Map \"{mapName}\" has been removed from your inventory!");
            }
            else
            {
                if (GameState.SaveData.Maps.Contains(mapName))
                {
                    FezugConsole.Print("You already have this map in your inventory.", FezugConsole.OutputType.Warning);
                    return false;
                }
                GameState.SaveData.Maps.Add(mapName);
                FezugConsole.Print($"Map \"{mapName}\" has been added to your inventory!");
            }


            return true;
        }
    }
}
