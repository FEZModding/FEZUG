using FezEngine.Tools;
using FezGame.Services;
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

        [ServiceDependency]
        public IPlayerManager PlayerManager { private get; set; }

        [ServiceDependency]
        public IGameLevelManager LevelManager { private get; set; }

        public void Initialize()
        {

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
            int width = (int)DrawingTools.DefaultFont.MeasureString("Position: (X:-999.999 Y:-999.999 Z:-999.999)").X * 2;

            DrawingTools.DrawRect(new Rectangle(2, 2, width, 170), new Color(0, 0, 0, 220));

            float padX = 10.0f;
            DrawText($"FEZUG {Fezug.Version}", new Vector2(padX, 0.0f));
            DrawText($"FEZUG {Fezug.Version}", new Vector2(padX, -1.0f));
            DrawText($"Level: {LevelManager.Name}", new Vector2(padX, 30.0f));

            string posX = PlayerManager.Position.X.ToString("0.000", CultureInfo.InvariantCulture);
            string posY = PlayerManager.Position.Y.ToString("0.000", CultureInfo.InvariantCulture);
            string posZ = PlayerManager.Position.Z.ToString("0.000", CultureInfo.InvariantCulture);

            string velX = PlayerManager.Velocity.X.ToString("0.000", CultureInfo.InvariantCulture);
            string velY = PlayerManager.Velocity.Y.ToString("0.000", CultureInfo.InvariantCulture);
            string velZ = PlayerManager.Velocity.Z.ToString("0.000", CultureInfo.InvariantCulture);

            DrawText($"Position: (X:{posX} Y:{posY} Z:{posZ})", new Vector2(padX, 60.0f));
            DrawText($"Velocity: (X:{velX} Y:{velY} Z:{velZ})", new Vector2(padX, 90.0f));
            DrawText($"State: {PlayerManager.Action}", new Vector2(padX, 120.0f));
        }
    }
}
