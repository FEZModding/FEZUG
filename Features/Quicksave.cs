using EasyStorage;
using FezEngine.Components;
using FezEngine.Tools;
using FezGame.Services;
using FezGame.Structure;
using FezGame.Tools;
using FezGame.Components;
using FEZUG.Features.Console;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using FezGame;
using Common;

namespace FEZUG.Features
{
    internal static class Quicksaving
    {
        public static readonly string SaveDirectory = "QuickSaves";
        
        public static string ValidateSaveFilePathOutOfArgs(string[] args)
        {
            if (args.Length != 1)
            {
                FezugConsole.Print($"Incorrect number of parameters: '{args.Length}'", FezugConsole.OutputType.Warning);
                return null;
            }

            string saveFileName = args[0];

            if(saveFileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                FezugConsole.Print($"Given save file name contains invalid characters.", FezugConsole.OutputType.Error);
                return null;
            }

            return Path.Combine(SaveDirectory, saveFileName);
        }

        internal class Quicksave : IFezugCommand
        {
            public string Name => "save";
            public string HelpText => "save <name> - creates a quick save file with given name";

            [ServiceDependency]
            public IGameStateManager GameState { private get; set; }

            [ServiceDependency]
            public IPlayerManager PlayerManager { private get; set; }

            public bool Execute(string[] args)
            {
                if (GameState.ActiveSaveDevice == null)
                {
                    FezugConsole.Print($"Can't save while the game is loading.", FezugConsole.OutputType.Error);
                    return false;
                }

                string saveFilePath = ValidateSaveFilePathOutOfArgs(args);
                if (saveFilePath == null) return false;

                var quicksaveDir = Path.Combine(Util.LocalConfigFolder, SaveDirectory);
                if (!Directory.Exists(quicksaveDir))
                {
                    Directory.CreateDirectory(quicksaveDir);
                }

                bool success = GameState.ActiveSaveDevice.Save(saveFilePath, delegate (BinaryWriter writer)
                {
                    var dummySave = new SaveData();
                    GameState.SaveData.CloneInto(dummySave);

                    // make sure to include current position in the save file
                    PlayerManager.RecordRespawnInformation(true);

                    if (GameState.SaveData.SinceLastSaved.HasValue)
                    {
                        GameState.SaveData.PlayTime += (DateTime.Now.Ticks - GameState.SaveData.SinceLastSaved.Value);
                    }
                    GameState.SaveData.SinceLastSaved = DateTime.Now.Ticks;
                    SaveFileOperations.Write(new CrcWriter(writer), GameState.SaveData);
                    GameState.SaveData = dummySave;
                });

                if (!success)
                {
                    FezugConsole.Print($"An error occurred when trying to create a quicksave.", FezugConsole.OutputType.Error);
                    return false;
                }

                FezugConsole.Print($"Saved quicksave \"{args[0]}\".");

                return true;
            }

            public List<string> Autocomplete(string[] args) => null;
        }



        internal class Quickload : IFezugCommand
        {
            public string Name => "load";
            public string HelpText => "load <name> - loads a quick save file with given name";

            [ServiceDependency]
            public IGameStateManager GameState { private get; set; }

            public bool Execute(string[] args)
            {
                if (GameState.ActiveSaveDevice == null)
                {
                    FezugConsole.Print($"Can't load while the game is loading.", FezugConsole.OutputType.Error);
                    return false;
                }

                string saveFilePath = ValidateSaveFilePathOutOfArgs(args);
                if (saveFilePath == null) return false;

                if (!GameState.ActiveSaveDevice.FileExists(saveFilePath))
                {
                    FezugConsole.Print($"Quicksave file \"{args[0]}\" not found.", FezugConsole.OutputType.Warning);
                    return false;
                }

                bool success = GameState.ActiveSaveDevice.Load(saveFilePath, delegate (BinaryReader reader)
                {
                    GameState.SaveData = SaveFileOperations.Read(new CrcReader(reader));
                });

                if (!success)
                {
                    FezugConsole.Print($"An error occurred when trying to load a quicksave.", FezugConsole.OutputType.Error);
                    return false;
                }

                WarpLevel.Warp(GameState.SaveData.Level, WarpLevel.WarpType.SaveChange);

                FezugConsole.Print($"Loaded quicksave \"{args[0]}\".");
                return true;
            }

            public List<string> Autocomplete(string[] args)
            {
                if (GameState.ActiveSaveDevice == null) return null;

                var quicksaveDir = Path.Combine(Util.LocalConfigFolder, SaveDirectory);

                if (!Directory.Exists(quicksaveDir))
                    return null;

                return Directory.GetFiles(quicksaveDir).Select(path => Path.GetFileName(path))
                    .Where(name => name.StartsWith(args[0])).ToList();
            }
        }
    }
}
