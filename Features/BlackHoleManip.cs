using Common;
using FezEngine.Services;
using FezEngine.Structure;
using FezEngine.Tools;
using FezGame;
using FezGame.Components;
using FezGame.Services;
using FEZUG.Features.Console;
using Microsoft.Xna.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace FEZUG.Features
{
    internal class BlackHoleManip : IConsoleCommand
    {
        public string Name => "blackholes";

        public string HelpText => "blackholes <on/off/lock/unlock> - manipulates the state of black holes in level";

        private bool cachedState = false;

        public bool Locked { get; set; }

        [ServiceDependency]
        public IGameStateManager GameState { private get; set; }

        [ServiceDependency]
        public IGameLevelManager LevelManager { private get; set; }

        public BlackHoleManip()
        {
            ServiceHelper.InjectServices(this);

            Locked = false;
            LevelManager.LevelChanged += OnLevelChange;
        }

        public List<string> Autocomplete(string args)
        {
            return new string[] { "on", "off", "lock", "unlock" }.Where(s => s.StartsWith(args)).ToList();
        }

        public bool Execute(string[] args)
        {
            if (args.Length != 1)
            {
                FEZUG.Console.Print($"Incorrect number of parameters: '{args.Length}'", ConsoleLine.OutputType.Warning);
                return false;
            }

            switch (args[0])
            {
                case "on":
                    EnableBlackHoles();
                    FEZUG.Console.Print($"Black holes have been enabled.");
                    break;
                case "off":
                    DisableBlackHoles();
                    FEZUG.Console.Print($"Black holes have been disabled.");
                    break;
                case "lock":
                    Locked = true;
                    cachedState = AreBlackHolesEnabled();
                    FEZUG.Console.Print($"Black holes have been locked in {(cachedState ? "enabled" : "disabled")} state.");
                    break;
                case "unlock":
                    Locked = false;
                    FEZUG.Console.Print($"Black holes state has been unlocked.");
                    break;
                default:
                    FEZUG.Console.Print($"Unknown parameter: '{args[0]}'", ConsoleLine.OutputType.Warning);
                    return false;
            }

            return true;
        }

        private bool AreBlackHolesEnabled()
        {
            var blackHole = LevelManager.Volumes.Values.Where((Volume x) => x.ActorSettings != null && x.ActorSettings.IsBlackHole);
            if (blackHole.Count() == 0) return false;
            return blackHole.First().Enabled;
        }

        private void EnableBlackHoles()
        {
            BlackHolesHost.Instance.EnableAll();
            cachedState = true;

            // if black holes were not enabled on start, they're invisible, and EnableAll doesn't change the visibility...
            var holes = typeof(BlackHolesHost).GetField("holes", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(BlackHolesHost.Instance) as IList;
            if (holes.Count == 0) return;

            var VisibleField = holes.GetType().GetGenericArguments()[0].GetProperty("Visible", BindingFlags.Public | BindingFlags.Instance);
            foreach (var hole in holes)
            {
                VisibleField.SetValue(hole, true);
            }

        }

        private void DisableBlackHoles()
        {
            BlackHolesHost.Instance.DisableAll();
            cachedState = false;
        }

        public void OnLevelChange()
        {
            if (!Locked) return;

            if(AreBlackHolesEnabled() && !cachedState)
            {
                DisableBlackHoles();
            }

            if (!AreBlackHolesEnabled() && cachedState)
            {
                EnableBlackHoles();
            }
        }
    }
}
