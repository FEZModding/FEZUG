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

namespace FEZUG.Features
{
    public class TextHud : IFezugFeature
    {
        private FezugVariable hud_level;
        private FezugVariable hud_position;
        private FezugVariable hud_velocity;
        private FezugVariable hud_state;
        private FezugVariable hud_viewpoint;

        private float lastWidth;
        private TimeSpan lastWidthUpdateTime;

        [ServiceDependency]
        public IPlayerManager PlayerManager { private get; set; }

        [ServiceDependency]
        public IGameLevelManager LevelManager { private get; set; }

        [ServiceDependency]
        public IGameCameraManager CameraManager { private get; set; }

        public void Initialize()
        {
            Func<string, string, FezugVariable> CreateHudVariable = delegate (string name, string desc)
            {
                return new FezugVariable(name, $"If set, enables {desc} text hud.", "0")
                {
                    SaveOnChange = true,
                    Min = 0,
                    Max = 1
                };
            };
            
            hud_level = CreateHudVariable("hud_level", "level");
            hud_position = CreateHudVariable("hud_position", "Gomez's position");
            hud_velocity = CreateHudVariable("hud_velocity", "Gomez's velocity");
            hud_state = CreateHudVariable("hud_state", "Gomez's state");
            hud_viewpoint = CreateHudVariable("hud_viewpoint", "camera viewpoint");
        }

        private void DrawText(string text, Vector2 pos)
        {
            DrawingTools.DrawText(text, pos, Color.White);
        }

        public void Update(GameTime gameTime)
        {

        }

        public void DrawLevel(GameTime gameTime) { }

        public void DrawHUD(GameTime gameTime)
        {
            var linesToDraw = new List<string>()
            {
                $"FEZUG {Fezug.Version}"
            };


            if (hud_level.ValueBool)
            {
                linesToDraw.Add($"Level: {LevelManager.Name}");
            }

            if (hud_position.ValueBool)
            {
                string posX = PlayerManager.Position.X.ToString("0.000", CultureInfo.InvariantCulture);
                string posY = PlayerManager.Position.Y.ToString("0.000", CultureInfo.InvariantCulture);
                string posZ = PlayerManager.Position.Z.ToString("0.000", CultureInfo.InvariantCulture);

                linesToDraw.Add($"Position: (X:{posX} Y:{posY} Z:{posZ})");
            }

            if (hud_velocity.ValueBool)
            {
                string velX = PlayerManager.Velocity.X.ToString("0.000", CultureInfo.InvariantCulture);
                string velY = PlayerManager.Velocity.Y.ToString("0.000", CultureInfo.InvariantCulture);
                string velZ = PlayerManager.Velocity.Z.ToString("0.000", CultureInfo.InvariantCulture);

                linesToDraw.Add($"Velocity: (X:{velX} Y:{velY} Z:{velZ})");
            }

            if (hud_state.ValueBool)
            {
                linesToDraw.Add($"State: {PlayerManager.Action}");
            }

            if (hud_viewpoint.ValueBool)
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
                linesToDraw.Add($"Viewpoint: {Viewpoint}");
            }

            float maxWidth = linesToDraw.Select(str => DrawingTools.DefaultFont.MeasureString(str).X * 2).Max();
            if(maxWidth > lastWidth || (gameTime.TotalGameTime - lastWidthUpdateTime).TotalSeconds > 1.0f)
            {
                lastWidthUpdateTime = gameTime.TotalGameTime;
                lastWidth = maxWidth;
            }

            float padX = 10.0f;
            DrawingTools.DrawRect(new Rectangle(2, 2, (int)(lastWidth + padX * 2), linesToDraw.Count * 30 + 20), new Color(0, 0, 0, 220));

            for(int i = 0; i < linesToDraw.Count; i++)
            {
                var line = linesToDraw[i];
                DrawText(line, new Vector2(padX, i * 30.0f));
                if (i == 0) DrawText(line, new Vector2(padX, (i*30.0f)-1.0f));
            }

        }
    }
}
