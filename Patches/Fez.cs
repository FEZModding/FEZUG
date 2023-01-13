using Common;
using FezEngine.Tools;
using FezGame.Components;
using FEZUG;
using FEZUG.Features;
using Microsoft.Xna.Framework;

namespace FezGame
{
    public class patch_Fez : Fez
    {
        public Fezug Fezug;

        protected extern void orig_Initialize();
        protected override void Initialize()
        {
            SpeedRunMode = true;

            orig_Initialize();

            ServiceHelper.AddComponent(Fezug = new Fezug(this));
            ServiceHelper.AddComponent(Fezug.Rendering);
            Logger.Log("FEZUG", "FEZUG initialized!");
        }
    }
}
