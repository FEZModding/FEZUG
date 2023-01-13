using System;
using FezEngine.Services;
using FezEngine.Structure;
using FezEngine.Tools;
using FezGame.Components;
using FezGame.Services;
using FEZUG.Features.Console;
using FEZUG.Helpers;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Xna.Framework;

namespace FEZUG.Features
{
    internal class LevelTimer : IFezugCommand, IFezugFeature
    {
        public string Name => "timer";

        public string HelpText => @"timer <start_level> <end_level> - creates a timer between two level entrances.\n 
                                    timer clear - clear the current timer.";

        private bool enabled = false;

        private bool active = false;

        private string startLevel = "";

        private string endLevel = "";

        public static List<string> LevelList { get; private set; }

        private TimeSpan lastTime = TimeSpan.Zero;
        private List<TimeSpan> timeHistory = new List<TimeSpan>();


        private string lastTimerString = "THIS IS A TEST";

        private GameTime currentTime = new GameTime();
        private GameTime startTime = new GameTime();


        [ServiceDependency]
        public ILevelManager LevelManager { private get; set; }

        [ServiceDependency]
        public IGameLevelManager GameLevelManager { private get; set; }

        public LevelTimer()
        {
            ServiceHelper.InjectServices(this);

            GameLevelManager.LevelChanged += OnLevelChange;
        }

        public void Initialize()
        {
            timeHistory.Clear();
        }

        private void RefreshLevelList()
        {
            LevelList = MemoryContentManager.AssetNames
                .Where(s => s.ToLower().StartsWith($"levels\\"))
                .Select(s => s.Substring("levels\\".Length)).ToList();
        }

        public List<string> Autocomplete(string[] args)
        {
            RefreshLevelList();
            return LevelList.Where(s => s.ToLower().StartsWith($"{args[args.Length - 1]}")).ToList();
        }

        public bool Execute(string[] args)
        {
            if (args.Length == 1 && args[0] == "clear")
            {
                FezugConsole.Print("Cleared current timer.");
                enabled = false;
                timeHistory.Clear();
                return false;
            }
            else if (args.Length != 2)
            {
                FezugConsole.Print("Invalid number of arguments.", FezugConsole.OutputType.Error);
                return false;
            }
            else
            {
                enabled = true;
                startLevel = args[0];
                endLevel = args[1];
                FezugConsole.Print("Timer enabled.");
                return true;
            }
        }

        public void StartTimer()
        {
            FezugConsole.Print("Timer started.");
            startTime = new GameTime(currentTime.TotalGameTime, currentTime.ElapsedGameTime);
        }

        public void StopTimer()
        {
            FezugConsole.Print("Timer stopped.");
            lastTime = new TimeSpan((currentTime.TotalGameTime - startTime.TotalGameTime).Ticks);
            timeHistory.Insert(0, lastTime);
        }

        public void OnLevelChange()
        {
            if (enabled)
            {
                if (!active && LevelManager.Name == startLevel.ToUpper())
                {
                    active = true;
                    StartTimer();
                }
                else if (active && LevelManager.Name == endLevel.ToUpper())
                {
                    active = false;
                    StopTimer();
                }
            }
        }

        public void Update(GameTime gameTime)
        {
            currentTime = gameTime;
        }

        public void DrawHUD(GameTime gameTime)
        {
            int textWidth = (int)DrawingTools.DefaultFont.MeasureString("99:99.999").X * 3;
            int textHeight = 45;
            int viewportWidth = DrawingTools.GetViewport().Width;
            int pad = 15;
            var timerPos = new Vector2(viewportWidth - textWidth - 300, 300 - pad);
            if (active)
            {
                DrawingTools.DrawRect(new Rectangle(viewportWidth - 300 - textWidth - pad, 300 - pad, textWidth + 2 * pad, textHeight + 2 * pad), new Color(0, 0, 0, 128));
                var timeDiff = (gameTime.TotalGameTime - startTime.TotalGameTime);
                var timerString = $"{timeDiff.Minutes:D2}:{timeDiff.Seconds:D2}.{timeDiff.Milliseconds:D3}";
                lastTimerString = timerString;
                DrawingTools.DrawText(timerString, timerPos, 0.0f, 3.0f, Color.White);
            }
            else if (timeHistory.Count > 0)
            {
                DrawingTools.DrawRect(new Rectangle(viewportWidth - 300 - textWidth - pad, 300 - pad, textWidth + 2 * pad, textHeight + 2 * pad), new Color(0, 0, 0, 128));
                if (timeHistory.Count > 1 && lastTime == timeHistory.Min())
                {
                    DrawingTools.DrawText(lastTimerString, timerPos, 0.0f, 3.0f, Color.Yellow);
                }
                else
                {
                    DrawingTools.DrawText(lastTimerString, timerPos, 0.0f, 3.0f, Color.White);
                }
            }
        }

        public void DrawLevel(GameTime gameTime)
        {

        }
    }
}

