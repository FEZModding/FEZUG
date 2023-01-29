using Common;
using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using FezGame.Services;
using FezEngine.Tools;
using FEZUG.Features.Console;

namespace FEZUG.Features
{
	internal class VolumeInfo : IFezugCommand
	{
		public string Name => "volumeinfo";

		public string HelpText => "volumeinfo: displays the ID of the volumes in the current level.";

		[ServiceDependency]
		public IGameLevelManager LevelManager { private get; set; }

		public VolumeInfo()
		{

		}

        public void Initialize() { }

		public List<string> Autocomplete(string[] _args) { return new List<string> { }; }

		public bool Execute(string[] _args)
		{
			string result = "";
			var Volumes = LevelManager.Volumes.Values;
			foreach (var volume in Volumes)
			{
				if(!(volume.ActorSettings != null && volume.ActorSettings.IsBlackHole))
				{
                    result += (volume.Id.ToString() + ", ");
                }
			}
			FezugConsole.Print(result);
			return true;
		}
    }

	internal class VolumeWarp : IFezugCommand
	{
        public string Name => "volumewarp";

        public string HelpText => "volumewarp <id> - warps to the specified volume.";

        [ServiceDependency]
        public IGameLevelManager LevelManager { private get; set; }

        [ServiceDependency]
        public IPlayerManager PlayerManager { private get; set; }

        public VolumeWarp()
        {

        }

        public void Initialize() { }

        public List<string> Autocomplete(string[] _args) { return new List<string> { }; }

        public bool Execute(string[] args)
        {
            if(args.Length != 1)
            {
                FezugConsole.Print("Wrong number of arguments.");
                return false;
            }
            var Volume = LevelManager.Volumes[int.Parse(args[0])];
            PlayerManager.Position = (Volume.From + Volume.To) / 2.0f;
            return true;
        }
    }
}

