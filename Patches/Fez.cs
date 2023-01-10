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
        public FEZUG.FEZUG SpeedrunTools;

        protected extern void orig_Initialize();
        protected override void Initialize()
        {
            SpeedRunMode = true;

            orig_Initialize();

            ServiceHelper.AddComponent(SpeedrunTools = new FEZUG.FEZUG(this));
            Logger.Log("FEZUG", "FEZUG initialized!");
        }
    }
}
