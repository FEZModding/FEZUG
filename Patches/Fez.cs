using Common;
using FezEngine.Tools;
using FezGame.Components;
using FEZUG;
using FEZUG.Features;
using Microsoft.Xna.Framework;
using System;
using System.Diagnostics;

namespace FezGame
{
    public class patch_Fez : Fez
    {
        private double timescaledGameTime;
        private double timescaledElapsedTime;

        public Fezug Fezug;

        protected extern void orig_Initialize();
        protected override void Initialize()
        {
            SpeedRunMode = true;

            orig_Initialize();

            ServiceHelper.AddComponent(Fezug = new Fezug(this));
            ServiceHelper.AddComponent(Fezug.Rendering);
            Logger.Log("FEZUG", "FEZUG initialized!");

            timescaledGameTime = 0.0f;
        }

        protected extern void orig_Update(GameTime gameTime);
        protected override void Update(GameTime gameTime)
        {
            if(gameTime.TotalGameTime.Ticks == 0)
            {
                timescaledGameTime = 0.0f;
            }
            timescaledElapsedTime = gameTime.ElapsedGameTime.TotalSeconds * Timescaler.Timescale;
            timescaledGameTime += timescaledElapsedTime;

            orig_Update(new GameTime(
                TimeSpan.FromSeconds(timescaledGameTime),
                TimeSpan.FromSeconds(timescaledElapsedTime)
            ));
        }

        protected extern void orig_Draw(GameTime gameTime);
        protected override void Draw(GameTime gameTime)
        {
            orig_Draw(new GameTime(
                TimeSpan.FromSeconds(timescaledGameTime),
                TimeSpan.FromSeconds(timescaledElapsedTime)
            ));
        }
    }
}
