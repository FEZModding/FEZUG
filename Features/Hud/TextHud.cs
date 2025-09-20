using FezEngine.Components;
using FezEngine.Services;
using FezEngine.Tools;
using FezGame.Services;
using FEZUG.Features.Console;
using FEZUG.Helpers;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FEZUG.Features.Hud
{
    public class TextHud : IFezugFeature
    {
        private List<(FezugVariable var, Func<string> provider)> hudVars = [];

        private FezugVariable hud_hide;

        private float lastWidth;
        private TimeSpan lastWidthUpdateTime;

        private HudPositioner Positioner;

        [ServiceDependency]
        public IPlayerManager PlayerManager { private get; set; }
        [ServiceDependency]
        public IInputManager InputManager { private get; set; }

        [ServiceDependency]
        public IGameLevelManager LevelManager { private get; set; }

        [ServiceDependency]
        public IGameCameraManager CameraManager { private get; set; }

        [ServiceDependency]
        public ITimeManager TimeManager { private get; set; }

        public void Initialize()
        {
            void CreateHudVariable(string name, string desc, Func<string> provider)
            {
                hudVars.Add((new FezugVariable(name, $"If set, enables {desc} text hud.", "0")
                {
                    SaveOnChange = true,
                    Min = 0,
                    Max = 1
                }, provider));
            }
            string FormatVector2(Vector2 vector3)
            {
                string posX = vector3.X.ToString("0.000", CultureInfo.InvariantCulture);
                string posY = vector3.Y.ToString("0.000", CultureInfo.InvariantCulture);
                return $"(X:{posX} Y:{posY})";
            }
            string FormatVector3(Vector3 vector3)
            {
                string posX = vector3.X.ToString("0.000", CultureInfo.InvariantCulture);
                string posY = vector3.Y.ToString("0.000", CultureInfo.InvariantCulture);
                string posZ = vector3.Z.ToString("0.000", CultureInfo.InvariantCulture);
                return $"(X:{posX} Y:{posY} Z:{posZ})";
            }

            CreateHudVariable("hud_fps", "frames per second", () => $"FPS: {_fps}");
            CreateHudVariable("hud_ups", "updates per second", () => $"UPS: {_ups}");
            CreateHudVariable("hud_level", "level", () => $"Level: {LevelManager.Name}");
            CreateHudVariable("hud_position", "Gomez's position", () => $"Position: {FormatVector3(PlayerManager.Position)}");
            CreateHudVariable("hud_velocity", "Gomez's velocity", () => $"Velocity: {FormatVector3(PlayerManager.Velocity)}");
            CreateHudVariable("hud_movement", "Input movement vector (left stick)", () => $"Movement: {FormatVector2(InputManager.Movement)}");
            CreateHudVariable("hud_freelook", "Input freelook vector (right stick)", () => $"Freelook: {FormatVector2(InputManager.FreeLook)}");
            CreateHudVariable("hud_state", "Gomez's state", () => $"State: {PlayerManager.Action}");
            CreateHudVariable("hud_viewpoint", "camera viewpoint", () => $"Viewpoint: {CameraManager.Viewpoint}");
            CreateHudVariable("hud_daytime", "Time of day", () => $"Time of day: {TimeManager.CurrentTime.TimeOfDay.ToString(@"hh':'mm':'ss")}");

            hud_hide = new FezugVariable("hud_hide", "If set, hides FEZUG HUD entirely when console is not opened.", "0")
            {
                SaveOnChange = true,
                Min = 0,
                Max = 1
            };

            Positioner = new HudPositioner("text", "global text", 0.0f, 0.0f);
        }

        private void DrawText(string text, Vector2 pos)
        {
            DrawingTools.DrawText(text, pos, Color.White);
        }

        private ulong _updatesDone = 0, _framesRendered = 0, _ups = 0, _fps = 0;
        private DateTime _lastTime = DateTime.Now;

        public void Update(GameTime gameTime)
        {
            _updatesDone++;
        }

        public void DrawLevel(GameTime gameTime) { }

        public void DrawHUD(GameTime gameTime)
        {
            _framesRendered++;
            if ((DateTime.Now - _lastTime).TotalSeconds >= 1)
            {
                // one second has elapsed 

                _fps = _framesRendered;
                _framesRendered = 0;
                _ups = _updatesDone;
                _updatesDone = 0;
                _lastTime = DateTime.Now;
            }

            if(hud_hide.ValueBool)
            {
                var console = Fezug.GetFeature<FezugConsole>();
                if (!console.Handler.Enabled) return;
            }


            var linesToDraw = new List<string>()
            {
                $"FEZUG {Fezug.Version}"
            };

            foreach (var hudVar in hudVars)
            {
                if(hudVar.var.ValueBool)
                {
                    linesToDraw.Add(hudVar.provider());
                }
            }

            {
                string Viewpoint = "";
                switch (CameraManager.Viewpoint)
                {
                    case FezEngine.Viewpoint.Back:
                        Viewpoint = "Back";
                        break;
                    case FezEngine.Viewpoint.Left:
                        Viewpoint = "Left";
                        break;
                    case FezEngine.Viewpoint.Right:
                        Viewpoint = "Right";
                        break;
                    case FezEngine.Viewpoint.Front:
                        Viewpoint = "Front";
                        break;
                    default:
                        Viewpoint = "Other";
                        break;
                }
            }

            float maxWidth = linesToDraw.Select(str => DrawingTools.DefaultFont.MeasureString(str).X * 2).Max();
            if(maxWidth > lastWidth || (gameTime.TotalGameTime - lastWidthUpdateTime).TotalSeconds > 5.0f)
            {
                lastWidthUpdateTime = gameTime.TotalGameTime;
                lastWidth = maxWidth;
            }

            float padX = 10.0f;
            float width = lastWidth + padX * 2;
            float height = linesToDraw.Count * 30 + 15;
            int margin = 5;
            var position = Positioner.GetPosition(width + 2 * margin, height + 2 * margin);

            DrawingTools.DrawRect(new Rectangle((int)(position.X + margin), (int)(position.Y + margin), (int)width, (int)height), new Color(10, 10, 10, 220));

            
            for (int i = 0; i < linesToDraw.Count; i++)
            {
                var line = linesToDraw[i];
                DrawText(line, position + new Vector2(padX, i * 30.0f));
                if (i == 0) DrawText(line, position + new Vector2(padX, (i*30.0f)-1.0f));
            }

        }
    }
}
