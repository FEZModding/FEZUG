using FezEngine.Structure;
using FezEngine.Tools;
using FezGame.Services;
using FEZUG.Features.Console;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FEZUG.Features
{
    internal class ProgressSet : IFezugCommand
    {
        public string Name => "progress";
        public string HelpText => "progress <flag/level/all> <name> <unlock/reset> - changes progress state for given flag or level.";

        [ServiceDependency]
        public IGameStateManager GameState { private get; set; }

        [ServiceDependency]
        public IGameLevelManager LevelManager { private get; set; }

        private int lastAssetNamesCount = 0;
        private List<string> _allowedFlagNames;
        public List<string> AllowedFlagNames
        {
            get{
                if (MemoryContentManager.AssetNames.Count() == lastAssetNamesCount) return _allowedFlagNames;
                lastAssetNamesCount = MemoryContentManager.AssetNames.Count();

                _allowedFlagNames = new List<string>
                {
                    "CanNewGamePlus", "IsNewGamePlus", "Finished32", "Finished64", "HasFPView", "HasStereo3D", "HasDoneHeartReboot",
                    "FezHidden", "HasHadMapHelp", "CanOpenMap", "AchievementCheatCodeDone", "MapCheatCodeDone", "AnyCodeDeciphered",
                    "Artifact.Tome", "Artifact.TriSkull", "Artifact.LetterCube", "Artifact.NumberCube"
                };

                foreach (var map in MemoryContentManager.AssetNames
                .Where(s => s.ToLower().StartsWith("other textures\\maps\\"))
                .Select(s => "Map." + s.ToUpper().Substring("other textures\\maps\\".Length)))
                {
                    AllowedFlagNames.Add(map);
                }

                return _allowedFlagNames;
            }
        }

        public bool Execute(string[] args)
        {
            if (args.Length < 1 || args.Length > 3)
            {
                FezugConsole.Print($"Incorrect number of parameters: '{args.Length}'", FezugConsole.OutputType.Warning);
                return false;
            }

            bool isLevel = args[0] == "level";
            bool isFlag = args[0] == "flag";
            bool isAll = args[0] == "all";

            if (!isLevel && !isFlag && !isAll)
            {
                FezugConsole.Print($"Invalid first parameter: '{args[1]}'. Should be either 'flag', 'level' or 'all'.", FezugConsole.OutputType.Warning);
                return false;
            }

            if (args.Length == 1)
            {
                if (isFlag)
                {
                    FezugConsole.Print($"List of available flags:");
                    FezugConsole.Print(String.Join(", ", AllowedFlagNames));
                    return true;
                }
                else
                {
                    FezugConsole.Print($"Incorrect number of parameters.", FezugConsole.OutputType.Warning);
                    return false;
                }
            }

            string propertyName = "";
            bool reset = false;

            if (args.Length == 2) {
                if ((isAll || isLevel) && (args[1] == "reset" || args[1] == "unlock"))
                {
                    reset = args[1] == "reset";
                    propertyName = LevelManager.Name;
                }
                else
                {
                    FezugConsole.Print($"Invalid usage of command.", FezugConsole.OutputType.Warning);
                    return false;
                }
            }
            if(args.Length == 3)
            {
                if (isAll)
                {
                    FezugConsole.Print($"Incorrect number of parameters.", FezugConsole.OutputType.Warning);
                    return false;
                }
                if (args[2] != "reset" && args[2] != "unlock")
                {
                    FezugConsole.Print($"Invalid last parameter: '{args[0]}'. Should be either 'reset' or 'unlock'.", FezugConsole.OutputType.Warning);
                    return false;
                }
                propertyName = args[1];
                reset = args[2] == "reset";
            }

            if (isLevel)
            {
                return SetLevelState(propertyName, !reset);
            }
            else if (isFlag)
            {
                return SetFlagState(propertyName, !reset);
            }
            else if(isAll)
            {
                // unlock all
                SetEveryLevelState(!reset);
                SetEveryFlagState(!reset);

                FezugConsole.Print($"Everything has been {(reset ? "reset" : "unlocked")}!");
            }

            return false;
        }

        private void UnlockLevel(string levelName)
        {
            if (!MemoryContentManager.AssetExists("Levels\\" + levelName.Replace('/', '\\')))
            {
                FezugConsole.Print($"Level with name '{levelName}' does not exist.", FezugConsole.OutputType.Warning);
                return;
            }

        }

        private bool SetLevelState(string levelName, bool unlock)
        {
            if (!MemoryContentManager.AssetExists("Levels\\" + levelName.Replace('/', '\\')))
            {
                FezugConsole.Print($"Level with name '{levelName}' does not exist.", FezugConsole.OutputType.Warning);
                return false;
            }

            if (!unlock)
            {
                GameState.SaveData.World.Remove(LevelManager.Name);
                FezugConsole.Print($"Progress in level '{levelName}' has been reset.");
            }
            else
            {
                FezugConsole.Print($"Unlocking levels hasn't been fully implemented yet.",  FezugConsole.OutputType.Warning);
                UnlockLevel(levelName);
            }
            return true;
        }

        private bool SetFlagState(string flagName, bool unlocked, bool output = true)
        {
            if (!AllowedFlagNames.Contains(flagName))
            {
                if(output) FezugConsole.Print($"Invalid flag: {flagName}!");
                return false;
            }

            if (flagName.StartsWith("Map."))
            {
                var mapName = flagName.Substring("Map.".Length);
                if (unlocked)
                {
                    if(GameState.SaveData.Maps.Count == 9)
                    {
                        if (output) FezugConsole.Print(
                            "The game doesn't support more than 9 maps at once! Cancelling!",
                            FezugConsole.OutputType.Error
                        );
                        return false;
                    }
                    GameState.SaveData.Maps.Add(mapName);
                }
                else GameState.SaveData.Maps.Remove(mapName);
            }
            else if (flagName.StartsWith("Artifact."))
            {
                ActorType artifact = (ActorType)Enum.Parse(typeof(ActorType), flagName.Substring("Artifact.".Length));
                if (unlocked) GameState.SaveData.Artifacts.Add(artifact);
                else GameState.SaveData.Artifacts.Remove(artifact);
            }
            else
            {
                var flagField = GameState.SaveData.GetType().GetField(flagName);
                flagField.SetValue(GameState.SaveData, unlocked);
            }

            if (output) FezugConsole.Print($"Flag {flagName} has been {(unlocked ? "unlocked" : "reset")}!");

            return true;
        }

        private void SetEveryLevelState(bool unlocked)
        {
            if (!unlocked)
            {
                GameState.SaveData.World.Clear();
            }
            else
            {
                foreach (var levelName in WarpLevel.LevelList)
                {
                    UnlockLevel(levelName);
                }
            }
        }

        private void SetEveryFlagState(bool unlocked)
        {
            foreach(var flag in AllowedFlagNames)
            {
                SetFlagState(flag, unlocked);
            }
        }


        public List<string> Autocomplete(string[] args)
        {
            if (args.Length == 1)
            {
                return new string[] { "flag", "level", "all" }.Where(s => s.StartsWith(args[0])).ToList();
            }
            else if (args.Length == 3 || args[0] == "all")
            {
                return new string[] { "unlock", "reset" }.Where(s => s.StartsWith(args[args.Length-1])).ToList();
            }
            else if (args.Length == 2)
            {
                if (args[0] == "level")
                {
                    var list = WarpLevel.Instance.Autocomplete(new string[]{ args[1] });
                    list.AddRange(
                        new string[] { "unlock", "reset" }.Where(s => s.StartsWith(args[1])).ToList()
                    );
                    return list;
                }
                else if (args[0] == "flag")
                {
                    return AllowedFlagNames.Where(s => s.StartsWith(args[1])).ToList();
                }
            }

            return null;
        }
    }
}
