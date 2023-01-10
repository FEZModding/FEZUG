using FezEngine.Structure;
using FezEngine.Tools;
using FezGame.Services;
using FEZUG.Features.Console;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FEZUG.Features
{
    internal class ProgressSet : IConsoleCommand
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
                FEZUG.Console.Print($"Incorrect number of parameters: '{args.Length}'", ConsoleLine.OutputType.Warning);
                return false;
            }

            bool isLevel = args[0] == "level";
            bool isFlag = args[0] == "flag";
            bool isAll = args[0] == "all";

            if (!isLevel && !isFlag && !isAll)
            {
                FEZUG.Console.Print($"Invalid first parameter: '{args[1]}'. Should be either 'flag', 'level' or 'all'.", ConsoleLine.OutputType.Warning);
                return false;
            }

            if (args.Length == 1)
            {
                if (isFlag)
                {
                    FEZUG.Console.Print($"List of available flags:");
                    FEZUG.Console.Print(String.Join(", ", AllowedFlagNames));
                    return true;
                }
                else
                {
                    FEZUG.Console.Print($"Incorrect number of parameters.", ConsoleLine.OutputType.Warning);
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
                    FEZUG.Console.Print($"Invalid usage of command.", ConsoleLine.OutputType.Warning);
                    return false;
                }
            }
            if(args.Length == 3)
            {
                if (isAll)
                {
                    FEZUG.Console.Print($"Incorrect number of parameters.", ConsoleLine.OutputType.Warning);
                    return false;
                }
                if (args[2] != "reset" && args[2] != "unlock")
                {
                    FEZUG.Console.Print($"Invalid last parameter: '{args[0]}'. Should be either 'reset' or 'unlock'.", ConsoleLine.OutputType.Warning);
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

                FEZUG.Console.Print($"Everything has been {(reset ? "reset" : "unlocked")}!");
            }

            return false;
        }

        private void UnlockLevel(string levelName)
        {
            if (!MemoryContentManager.AssetExists("Levels\\" + levelName.Replace('/', '\\')))
            {
                FEZUG.Console.Print($"Level with name '{levelName}' does not exist.", ConsoleLine.OutputType.Warning);
                return;
            }

        }

        private bool SetLevelState(string levelName, bool unlock)
        {
            if (!MemoryContentManager.AssetExists("Levels\\" + levelName.Replace('/', '\\')))
            {
                FEZUG.Console.Print($"Level with name '{levelName}' does not exist.", ConsoleLine.OutputType.Warning);
                return false;
            }

            if (!unlock)
            {
                GameState.SaveData.World.Remove(LevelManager.Name);
                FEZUG.Console.Print($"Progress in level '{levelName}' has been reset.");
            }
            else
            {
                FEZUG.Console.Print($"Unlocking levels hasn't been fully implemented yet.",  ConsoleLine.OutputType.Warning);
                UnlockLevel(levelName);
            }
            return true;
        }

        private bool SetFlagState(string flagName, bool unlocked, bool output = true)
        {
            if (!AllowedFlagNames.Contains(flagName))
            {
                if(output) FEZUG.Console.Print($"Invalid flag: {flagName}!");
                return false;
            }

            if (flagName.StartsWith("Map."))
            {
                var mapName = flagName.Substring("Map.".Length);
                if (unlocked)
                {
                    if(GameState.SaveData.Maps.Count == 9)
                    {
                        if (output) FEZUG.Console.Print(
                            "The game doesn't support more than 9 maps at once! Cancelling!",
                            ConsoleLine.OutputType.Error
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

            if (output) FEZUG.Console.Print($"Flag {flagName} has been {(unlocked ? "unlocked" : "reset")}!");

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


        public List<string> Autocomplete(string argsFull)
        {
            var args = argsFull.Split(' ');

            var prefix = argsFull.Substring(0, argsFull.Length - args[args.Length - 1].Length);

            List<string> returnList = null;

            if (args.Length == 1)
            {
                returnList = new string[] { "flag", "level", "all" }.Where(s => s.StartsWith(args[0])).ToList();
                
            }
            else if (args.Length == 3 || args[0] == "all")
            {
                returnList = new string[] { "unlock", "reset" }.Where(s => s.StartsWith(args[args.Length-1])).ToList();
            }
            else if (args.Length == 2)
            {
                if (args[0] == "level")
                {
                    returnList = WarpLevel.Instance.Autocomplete(args[1]);
                    returnList.AddRange(
                        new string[] { "unlock", "reset" }.Where(s => s.StartsWith(args[1])).ToList()
                    );
                }
                else if (args[0] == "flag")
                {
                    returnList = AllowedFlagNames.Where(s => s.StartsWith(args[1])).ToList();
                }
            }

            if (returnList != null)
            {
                return returnList.Select(s => prefix + s).ToList();
            }
            else return null;
        }
    }
}
