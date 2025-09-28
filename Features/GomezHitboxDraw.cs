using FezEngine.Structure;
using FezEngine.Tools;
using FEZUG.Helpers;
using Microsoft.Xna.Framework;

namespace FEZUG.Features
{
    internal class GomezHitboxDraw : WireframeDraw
    {
        private Mesh GomezBoundingBox;
        public static GomezHitboxDraw Instance;

        public GomezHitboxDraw() : base()
        {
            Instance = this;
        }

        protected override Mesh[] RefreshBoundingBoxMeshs()
        {
            return [GomezBoundingBox = CreateHitboxMesh(Color.Red)];
        }

        public override void DrawLevel(GameTime gameTime)
        {
            if (!WireframesEnabled || GameState.Loading || LevelManager.Name == null) return;

            DrawingTools.GraphicsDevice.PrepareStencilWrite(StencilMask.Gomez);

            var gomezbb = GomezBoundingBox;
            gomezbb.Position = PlayerManager.Position;
            gomezbb.Scale = PlayerManager.Size;
            gomezbb.Draw();

            DrawingTools.GraphicsDevice.PrepareStencilWrite(StencilMask.None);
        }

        protected override void RefreshLevelList() { }




        class GomezHitboxDrawToggleCommand : WireframesDrawToggleCommand
        {
            protected override string WhatFor => "Gomez";
            public override bool WireframesEnabled
            {
                get => Instance.WireframesEnabled;
                set => Instance.WireframesEnabled = value;
            }
            public override Mesh[] BoundingBoxes => Instance.BoundingBoxes;
        }
    }
}
