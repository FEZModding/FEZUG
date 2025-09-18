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

		public List<string> Autocomplete(string[] _args) { return []; }

		public bool Execute(string[] args)
		{
			string result = "";
            if(args.Length > 0)
            {
                if(int.TryParse(args[0], out int volumeId))
                {
                    if (LevelManager.Volumes.TryGetValue(volumeId, out var volume))
                    {
                        FezugConsole.Print($"From: {volume.From}");
                        FezugConsole.Print($"To: {volume.To}");
                        FezugConsole.Print($"Bounding Box: {volume.BoundingBox}");
                        FezugConsole.Print($"Enabled: {volume.Enabled}");
                        FezugConsole.Print($"Orientations: {string.Join(", ", volume.Orientations.OrderBy(a=>a).ToArray())}");
                        if (volume.ActorSettings != null)
                        {
                            var actorSettings = volume.ActorSettings;
                            if (actorSettings.IsPointOfInterest)
                            {
                                FezugConsole.Print($"IsPointOfInterest: {actorSettings.IsPointOfInterest}");
                            }
                            if (actorSettings.IsSecretPassage)
                            {
                                FezugConsole.Print($"IsSecretPassage: {actorSettings.IsSecretPassage}");
                            }
                            if (actorSettings.IsBlackHole)
                            {
                                FezugConsole.Print($"IsBlackHole: {actorSettings.IsBlackHole}");
                            }
                            if (actorSettings.WaterLocked)
                            {
                                FezugConsole.Print($"WaterLocked: {actorSettings.WaterLocked}");
                            }
                            if (actorSettings.NeedsTrigger)
                            {
                                FezugConsole.Print($"NeedsTrigger: {actorSettings.NeedsTrigger}");
                            }
                            if(actorSettings.CodePattern != null && actorSettings.CodePattern.Length > 0)
                            {
                                FezugConsole.Print($"CodePattern: {string.Join(", ", actorSettings.CodePattern)}");
                            }
                        }
                        return true;
                    }
                }
                FezugConsole.Print($"Unknown volume ID: {args[0]}");
                return false;
            }
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

        public List<string> Autocomplete(string[] _args) { return []; }

        public bool Execute(string[] args)
        {
            if(args.Length != 1)
            {
                FezugConsole.Print("Wrong number of arguments.");
                return false;
            }

            int volumeId = int.Parse(args[0]);

            if(!LevelManager.VolumeExists(volumeId))
            {
                FezugConsole.Print("Volume ID given does not exist in the current level or is a blackhole.");
                return false;
            }

            var Volume = LevelManager.Volumes[int.Parse(args[0])];
            PlayerManager.IgnoreFreefall = true;
            PlayerManager.Position = (Volume.From + Volume.To) / 2.0f;
            return true;
        }
    }
}

