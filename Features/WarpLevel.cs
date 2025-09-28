using FezEngine.Components;
using FezEngine.Services;
using FezEngine.Structure;
using FezEngine.Tools;
using FezGame;
using FezGame.Services;
using FezGame.Structure;
using FEZUG.Features.Console;
using Microsoft.Xna.Framework;
using System.Reflection;

namespace FEZUG.Features
{
    internal class WarpLevel : IFezugCommand
    {
        public string Name => "warp";
        public string HelpText => "warp <level> - warps you to level with given name";

		public enum WarpType
        {
			InSession,
			SaveChange
        }


		public static List<string> LevelList
        {
            get
            {
				return [.. MemoryContentManager.AssetNames
				.Where(s => s.ToLower().StartsWith($"levels\\"))
				.Select(s => s.Substring("levels\\".Length))];
			}
		}

		[ServiceDependency]
		public IPlayerManager PlayerManager { private get; set; }

		[ServiceDependency]
		public IGameStateManager GameState { private get; set; }

		[ServiceDependency]
		public IGameLevelManager LevelManager { private get; set; }

		[ServiceDependency]
		public IDefaultCameraManager CameraManager { private get; set; }

		[ServiceDependency]
		public ILevelMaterializer LevelMaterializer { get; set; }

		public static WarpLevel Instance { get; private set; }

		public WarpLevel()
        {
			Instance = this;
		}

		public bool Execute(string[] args)
        {
			if (args.Length > 1)
            {
				FezugConsole.Print($"Incorrect number of parameters: '{args.Length}'", FezugConsole.OutputType.Warning);
				return false;
			}

			if(args.Length == 0)
            {
				FezugConsole.Print("List of available levels:");
				FezugConsole.Print(string.Join(", ", LevelList));
				return true;
			}

			string levelName = args[0];

			if (!MemoryContentManager.AssetExists("Levels\\" + levelName.Replace('/', '\\')))
			{
				FezugConsole.Print($"Couldn't find level with name '{levelName}'", FezugConsole.OutputType.Warning);
				return false;
			}

			Warp(levelName);

			FezugConsole.Print($"Warping into '{levelName}'...");

			return true;
        }

		public void WarpInternal(string levelName, WarpType warpType)
        {
			/* pre-warp safety measures */

			// force menu cube closed
			{
				GameState.SkipRendering = false;
				LevelManager.SkipInvalidation = false;
				GameState.SkyOpacity = 1f;
				var matches = ServiceHelper.Game.Components.Where(c => c.GetType().Name == "MenuCube");
				if (matches.Any())
				{
					var menuCube = matches.First();
					IEnumerable<ArtObjectInstance> ArtifactAOs = (IEnumerable<ArtObjectInstance>)
							typeof(Fez).Assembly.GetType("FezGame.Components.MenuCube")
							.GetField("ArtifactAOs", BindingFlags.Instance | BindingFlags.NonPublic)
							.GetValue(menuCube);

					foreach (ArtObjectInstance artifactAO in ArtifactAOs)
					{
						artifactAO.SoftDispose();
						LevelManager.ArtObjects.Remove(artifactAO.Id);
					}
					ServiceHelper.Get<FezEngine.Services.Scripting.IGameService>().CloseScroll(null);
					GameState.InMenuCube = false;
					GameState.DisallowRotation = false;
					ServiceHelper.RemoveComponent(menuCube);
				}
			}

            // make sure no stuff from previous save remains after switching the save.
            List<IWaiter> waitersToCancel = [.. ServiceHelper.Game.Components.Where(comp => comp is IWaiter).Select(comp => comp as IWaiter)];
			foreach (IWaiter waiter in waitersToCancel)
			{
				waiter.Cancel();
			}

			// dirty hack to prevent bit collection across saves (it's all internal or private... kill me)
			if(warpType == WarpType.SaveChange)
            {
				Type SplitUpCubeHostType = Assembly.GetAssembly(typeof(Fez)).GetType("FezGame.Components.SplitUpCubeHost");
				var SplitUpCubeHost = ServiceHelper.Game.Components.First(c => c.GetType() == SplitUpCubeHostType);
				if (SplitUpCubeHost != null)
				{
					// reset the cube assembly
					SplitUpCubeHostType.GetField("AssembleScheduled", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(SplitUpCubeHost, false);

					// clear currently collected bits
					var TrackedCollectsField = SplitUpCubeHostType.GetField("TrackedCollects", BindingFlags.NonPublic | BindingFlags.Instance);
					var TrackedCollects = TrackedCollectsField.GetValue(SplitUpCubeHost);
					MethodInfo TrackedCollectsClear = TrackedCollects.GetType().GetMethod("Clear");
					TrackedCollectsClear.Invoke(TrackedCollects, []);
				}
			}

			// levels are uppercase.
			levelName = levelName.ToUpper();

			// make sure forced treasure is not set
			PlayerManager.ForcedTreasure = null;

			// prevent ChangeLevel from adjusting you to linked doors
			LevelManager.Name = null;

			// make sure first-person mode is disabled
			GameState.InFpsMode = false;
			PlayerManager.GomezOpacity = 1f;

			// reset player's warping state (yes, smaller gate miraculously works fine, only the big one needs fixing)
			if (PlayerManager.Action == ActionType.GateWarp)
            {
				Type GateWarpType = Assembly.GetAssembly(typeof(Fez)).GetType("FezGame.Components.Actions.GateWarp");
				var GateWarp = ServiceHelper.Game.Components.First(c => c.GetType() == GateWarpType);
				if (GateWarp != null)
				{
					var PhaseField = GateWarpType.GetField("Phase", BindingFlags.NonPublic | BindingFlags.Instance);
					PhaseField.SetValue(GateWarp, 0);
				}
				PlayerManager.Action = ActionType.Idle;
			}

			PlayerManager.Hidden = false;

			// level change logic
			GameState.Loading = true;
            // setting PlayerManager.Action to WakingUp fixes an infinite loop when menu cube is open
            var lastAction = PlayerManager.Action;
			PlayerManager.Action = ActionType.WakingUp;
            LevelManager.ChangeLevel(levelName);
            PlayerManager.Action = lastAction;
            CameraManager.Center = PlayerManager.Position + Vector3.Up * PlayerManager.Size.Y / 2f + Vector3.UnitY;
			CameraManager.SnapInterpolation();
			LevelMaterializer.CullInstances();
			GameState.ScheduleLoadEnd = true;

			PlayerManager.CanControl = true;
			GameState.ForceTimePaused = false;
		}

		public static void Warp(string levelName, WarpType warpType = WarpType.InSession)
        {
			Instance.WarpInternal(levelName, warpType);
        }

        public List<string> Autocomplete(string[] args)
        {
			return [.. LevelList.Where(s => s.ToLower().StartsWith($"{args[0]}"))];
		}
    }
}
