using Common;
using FezEngine.Components;
using FezEngine.Services;
using FezEngine.Services.Scripting;
using FezEngine.Tools;
using FezGame;
using FezGame.Services;
using FezGame.Structure;
using FEZUG.Features.Console;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace FEZUG.Features
{
    internal class WarpLevel : IFezugCommand
    {
        public string Name => "warp";
        public string HelpText => "warp <level> - warps you to level with given name";

		public static List<string> LevelList { get; private set; }

		public enum WarpType
        {
			InSession,
			SaveChange
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

			RefreshLevelList();
		}

		private void RefreshLevelList()
        {
			LevelList = MemoryContentManager.AssetNames
				.Where(s => s.ToLower().StartsWith($"levels\\"))
				.Select(s => s.Substring("levels\\".Length)).ToList();
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
				RefreshLevelList();
				FezugConsole.Print("List of available levels:");
				FezugConsole.Print(String.Join(", ", LevelList));
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

			RefreshLevelList();

			return true;
        }

		public void WarpInternal(string levelName, WarpType warpType)
        {
			/* pre-warp safety measures */

			// make sure no stuff from previous save remains after switching the save.
			IWaiter waiter;
			while ((waiter = ServiceHelper.Get<IWaiter>()) != null)
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
					var TrackedCollectsField = SplitUpCubeHostType.GetField("TrackedCollects", BindingFlags.NonPublic | BindingFlags.Instance);
					var TrackedCollects = TrackedCollectsField.GetValue(SplitUpCubeHost);
					MethodInfo TrackedCollectsClear = TrackedCollects.GetType().GetMethod("Clear");
					TrackedCollectsClear.Invoke(TrackedCollects, new object[] { });
				}
			}

			// levels are uppercase.
			levelName = levelName.ToUpper();

			// make sure forced treasure is not set
			PlayerManager.ForcedTreasure = null;

			// prevent ChangeLevel from adjusting you to linked doors
			LevelManager.Name = null;

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
			LevelManager.ChangeLevel(levelName);
			CameraManager.Center = PlayerManager.Position + Vector3.Up * PlayerManager.Size.Y / 2f + Vector3.UnitY;
			CameraManager.SnapInterpolation();
			LevelMaterializer.CullInstances();
			GameState.ScheduleLoadEnd = true;

			PlayerManager.CanControl = true;
		}

		public static void Warp(string levelName, WarpType warpType = WarpType.InSession)
        {
			Instance.WarpInternal(levelName, warpType);
        }

        public List<string> Autocomplete(string[] args)
        {
			RefreshLevelList();
			return LevelList.Where(s => s.ToLower().StartsWith($"{args[0]}")).ToList();
		}
    }
}
