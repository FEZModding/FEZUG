
using FezEngine.Components;
using FezEngine.Tools;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FEZUG.Helpers
{
    internal static class DrawingTools
    {
        public static IFontManager FontManager { get; private set; }
        public static GraphicsDevice GraphicsDevice { get; private set; }
        public static SpriteBatch Batch { get; private set; }

        private static Texture2D fillTexture;

        public static SpriteFont DefaultFont { get; set; }
        public static float DefaultFontSize { get; set; }

        public static void Init()
        {
            DrawActionScheduler.Schedule(delegate
            {
                FontManager = ServiceHelper.Get<IFontManager>();
                GraphicsDevice = ServiceHelper.Get<IGraphicsDeviceService>().GraphicsDevice;
                Batch = new SpriteBatch(GraphicsDevice);
                DefaultFont = FontManager.Big;
                DefaultFontSize = FontManager.BigFactor;

                fillTexture = new Texture2D(GraphicsDevice, 1, 1, false, SurfaceFormat.Color);
                fillTexture.SetData([new Color(255, 255, 255)]);
            });
        }

        public static Viewport GetViewport()
        {
            return GraphicsDevice.Viewport;
        }

        public static void BeginBatch()
        {
            Batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone);
        }

        public static void EndBatch()
        {
            Batch.End();
        }

        public static void DrawRect(Rectangle rect, Color color)
        {
            Batch.Draw(fillTexture, rect, color);
        }

        public static void DrawText(string text, Vector2 position)
        {
            DrawText(text, position, Color.White);
        }

        public static void DrawText(string text, Vector2 position, Color color)
        {
            DrawText(text, position, 0.0f, DefaultFontSize, Vector2.Zero, color);
        }

        public static void DrawText(string text, Vector2 position, float rotation, float scale, Color color)
        {
            DrawText(text, position, rotation, scale, Vector2.Zero, color);
        }

        public static void DrawText(string text, Vector2 position, float rotation, float scale, Vector2 origin, Color color)
        {
            Batch.DrawString(DefaultFont, text, position, color,
                rotation, origin, scale, SpriteEffects.None, 0f
            );
        }

        public static void DrawLineSegment(Vector2 point1, Vector2 point2, Color color, int lineWidth)
        {
            float angle = (float)Math.Atan2(point2.Y - point1.Y, point2.X - point1.X);
            float length = Vector2.Distance(point1, point2);

            Batch.Draw(
                fillTexture,
                point1,
                null,
                color,
                angle,
                Vector2.Zero, // Origin of the texture, usually top-left for a line
                new Vector2(length, lineWidth), // Scale X by length, Y by desired line thickness
                SpriteEffects.None,
                0f
            );
        }

    }
}
