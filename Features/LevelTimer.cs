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
using FEZUG.Features.Hud;

namespace FEZUG.Features
{
    internal class LevelTimer : IFezugCommand, IFezugFeature
    {
        public string Name => "timer";

        public string HelpText => "timer <start_level> <end_level> - creates a timer between two level entrances." 
            +"\ntimer clear - clear the current timer.";

        private bool enabled = false;

        private bool active = false;

        private string startLevel = "";

        private string endLevel = "";

        private TimeSpan lastTime = TimeSpan.Zero;
        private List<TimeSpan> timeHistory = new List<TimeSpan>();


        private string lastTimerString = "THIS IS A TEST";

        private GameTime currentTime = new GameTime();
        private GameTime startTime = new GameTime();

        private HudPositioner Positioner;

        [ServiceDependency]
        public ILevelManager LevelManager { private get; set; }

        [ServiceDependency]
        public IGameLevelManager GameLevelManager { private get; set; }

        public LevelTimer()
        {
            ServiceHelper.InjectServices(this);

            GameLevelManager.LevelChanged += OnLevelChange;

            Positioner = new HudPositioner("timer", "level timer", 0.5f, 0.0f);
        }

        public void Initialize()
        {
            timeHistory.Clear();
        }

        public List<string> Autocomplete(string[] args)
        {
            if (args.Length > 2) return null;
            var autocompleteList = WarpLevel.LevelList
                .Where(s => s.StartsWith($"{args[args.Length - 1]}", StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (args.Length == 1 && "clear".StartsWith(args[0], StringComparison.OrdinalIgnoreCase)) autocompleteList.Add("clear");
            return autocompleteList;
        }

        public bool Execute(string[] args)
        {
            if (args.Length == 1 && args[0] == "clear")
            {
                FezugConsole.Print("Cleared current timer.");
                enabled = false;
                active = false;
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
            if (!enabled) return;

            var defaultTimerString = "00:00.000";
            var timerString = defaultTimerString;
            var timerColor = Color.White;

            var textScale = 3.0f;
            var textWidth = (int)(DrawingTools.DefaultFont.MeasureString(defaultTimerString).X * textScale);
            var textHeight = 45;

            if (active)
            {
                var timeDiff = (gameTime.TotalGameTime - startTime.TotalGameTime);
                timerString = $"{timeDiff.Minutes:D2}:{timeDiff.Seconds:D2}.{timeDiff.Milliseconds:D3}";
                lastTimerString = timerString;
            }
            else if (timeHistory.Count > 0)
            {
                timerString = lastTimerString;
                if (timeHistory.Count > 1 && lastTime == timeHistory.Min())
                {
                    timerColor = Color.Yellow;
                }
            }

            var margin = 5;
            var width = textWidth + 10;
            var height = textHeight + 20;
            var position = Positioner.GetPosition(width + 2 * margin, height + 2 * margin);

            DrawingTools.DrawRect(new Rectangle((int)(position.X + margin), (int)(position.Y + margin), width, height), new Color(10, 10, 10, 220));

            for (var i = 0; i < timerString.Length; i++)
            {
                var currentCharOffset = DrawingTools.DefaultFont.MeasureString(defaultTimerString.Substring(0,i)).X * textScale + 12;
                DrawingTools.DrawText(timerString[i].ToString(), position + Vector2.UnitX * currentCharOffset, 0.0f, textScale, timerColor);
            }
        }

        public void DrawLevel(GameTime gameTime) { }
    }
}

