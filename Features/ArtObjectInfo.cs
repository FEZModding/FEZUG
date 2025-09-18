using Microsoft.Xna.Framework;
using FezGame.Services;
using FezEngine.Tools;
using FEZUG.Features.Console;

namespace FEZUG.Features
{
	internal class ArtObjectInfo : IFezugCommand
	{
		public string Name => "artobjectinfo";

		public string HelpText => "artobjectinfo: displays the ID of the art objects in the current level.";

		[ServiceDependency]
		public IGameLevelManager LevelManager { private get; set; }

		public ArtObjectInfo()
		{

		}

        public void Initialize() { }

		public List<string> Autocomplete(string[] _args) { return []; }

		public bool Execute(string[] args)
		{
			string result = "";
            if(args.Length > 0)
            {
                if(int.TryParse(args[0], out int artobjectID))
                {
                    //Note: some art objects get removed upon loading the level, and some art objects are added to the level after it's loaded.

                    if (LevelManager.ArtObjects.TryGetValue(artobjectID, out var artObjectInstance))
                    {
                        var artObject = artObjectInstance.ArtObject;
                        FezugConsole.Print($"ArtObject Name: {artObjectInstance.ArtObjectName}");
                        FezugConsole.Print($"Position: {artObjectInstance.Position}");
                        FezugConsole.Print($"Size: {artObject.Size}");
                        if (artObjectInstance.Scale != Vector3.One)
                        {
                            FezugConsole.Print($"Scale: {artObjectInstance.Scale}");
                        }
                        FezugConsole.Print($"Rotation: {artObjectInstance.Rotation}");
                        FezugConsole.Print($"Bounds: {artObjectInstance.Bounds}");
                        if (!artObjectInstance.Enabled)
                        {
                            FezugConsole.Print($"Enabled: {artObjectInstance.Enabled}");
                        }
                        if (artObjectInstance.ActorSettings != null)
                        {
                            var actorSettings = artObjectInstance.ActorSettings;
                            if (actorSettings.Inactive)
                            {
                                FezugConsole.Print($"Inactive: {actorSettings.Inactive}");
                            }
                            if (actorSettings.ContainedTrile != FezEngine.Structure.ActorType.None)
                            {
                                FezugConsole.Print($"ContainedTrile: {actorSettings.ContainedTrile}");
                            }
                            if (actorSettings.AttachedGroup != null)
                            {
                                FezugConsole.Print($"AttachedGroup: {actorSettings.AttachedGroup}");
                            }
                            if (actorSettings.SpinOffset != 0)
                            {
                                FezugConsole.Print($"SpinOffset: {actorSettings.SpinOffset}");
                            }
                            if (actorSettings.SpinEvery != 0)
                            {
                                FezugConsole.Print($"SpinEvery: {actorSettings.SpinEvery}");
                            }
                            if (actorSettings.SpinView != FezEngine.Viewpoint.None)
                            {
                                FezugConsole.Print($"SpinView: {actorSettings.SpinView}");
                            }
                            if (actorSettings.OffCenter)
                            {
                                FezugConsole.Print($"OffCenter: {actorSettings.OffCenter}");
                            }
                            if (actorSettings.RotationCenter != Vector3.Zero)
                            {
                                FezugConsole.Print($"RotationCenter: {actorSettings.RotationCenter}");
                            }
                            if(actorSettings.VibrationPattern != null && actorSettings.VibrationPattern.Length > 0)
                            {
                                FezugConsole.Print($"VibrationPattern: {string.Join(", ", actorSettings.VibrationPattern)}");
                            }
                            if(actorSettings.CodePattern != null && actorSettings.CodePattern.Length > 0)
                            {
                                FezugConsole.Print($"CodePattern: {string.Join(", ", actorSettings.CodePattern)}");
                            }
                            //if (actorSettings.Segment != null)
                            //{
                            //    FezugConsole.Print($"Segment: {actorSettings.Segment}");
                            //}
                            if (actorSettings.NextNode != null)
                            {
                                FezugConsole.Print($"NextNode: {actorSettings.NextNode}");
                            }
                            if (actorSettings.DestinationLevel != null)
                            {
                                FezugConsole.Print($"DestinationLevel: {actorSettings.DestinationLevel}");
                            }
                            if (actorSettings.TreasureMapName != null)
                            {
                                FezugConsole.Print($"TreasureMapName: {actorSettings.TreasureMapName}");
                            }
                            if (actorSettings.TimeswitchWindBackSpeed != 0f)
                            {
                                FezugConsole.Print($"TimeswitchWindBackSpeed: {actorSettings.TimeswitchWindBackSpeed}");
                            }
                            if (actorSettings.InvisibleSides != null && actorSettings.InvisibleSides.Count > 0)
                            {
                                FezugConsole.Print($"InvisibleSides: {string.Join(", ", actorSettings.InvisibleSides)}");
                            }
                            //if (actorSettings.NextNodeAo != null)
                            //{
                            //    FezugConsole.Print($"NextNodeAo: {actorSettings.NextNodeAo}");
                            //}
                            //if (actorSettings.PrecedingNodeAo != null)
                            //{
                            //    FezugConsole.Print($"PrecedingNodeAo: {actorSettings.PrecedingNodeAo}");
                            //}
                            if (actorSettings.ShouldMoveToEnd)
                            {
                                FezugConsole.Print($"ShouldMoveToEnd: {actorSettings.ShouldMoveToEnd}");
                            }
                            if (actorSettings.ShouldMoveToHeight != null)
                            {
                                FezugConsole.Print($"ShouldMoveToHeight: {actorSettings.ShouldMoveToHeight}");
                            }
                        }
                        return true;
                    }
                }
                FezugConsole.Print($"Unknown art object ID: {args[0]}");
                return false;
            }
            result += string.Join(", ", LevelManager.ArtObjects.Keys);
			FezugConsole.Print(result);
			return true;
		}
    }

	internal class ArtObjectWarp : IFezugCommand
	{
        public string Name => "artobjectwarp";

        public string HelpText => "artobjectwarp <id> - warps to the specified art object.";

        [ServiceDependency]
        public IGameLevelManager LevelManager { private get; set; }

        [ServiceDependency]
        public IPlayerManager PlayerManager { private get; set; }

        public ArtObjectWarp()
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

            if(int.TryParse(args[0], out int artobjectID) && LevelManager.ArtObjects.TryGetValue(artobjectID, out var artObjectInstance))
            {
                PlayerManager.IgnoreFreefall = true;
                PlayerManager.Position = artObjectInstance.Bounds.GetCenter();
                return true;
            }
            FezugConsole.Print("Art Object ID given does not exist in the current level.");
            return false;

        }
    }
}

