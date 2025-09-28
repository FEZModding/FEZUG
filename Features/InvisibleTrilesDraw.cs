using FezEngine;
using FezEngine.Effects;
using FezEngine.Services;
using FezEngine.Structure;
using FezEngine.Tools;
using FezGame.Services;
using FEZUG.Features.Console;
using FEZUG.Helpers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FEZUG.Features
{
    internal class InvisibleTrilesDraw : WireframeDraw
    {
        public static InvisibleTrilesDraw Instance;

        public InvisibleTrilesDraw() : base()
        {
            Instance = this;
        }

        private enum InvisibleType
        {
            OneFace,
            Lightning,
            Crystal
        }

        private Group oneFaceGroup;
        private Dictionary<TrileEmplacement, InvisibleType> invisibleTriles = [];

        private Mesh[] TrileBoundingBoxes;

        protected override Mesh[] RefreshBoundingBoxMeshs()
        {
            Color[] trileColors =
            [
                Color.Gray,
                Color.White,
                Color.Magenta
            ];
            int colorCount = trileColors.Length;
            TrileBoundingBoxes = new Mesh[3];
            for (var i = 0; i < colorCount; i++)
            {
                TrileBoundingBoxes[i] = CreateHitboxMesh(trileColors[i]);
                if (i == 0)
                {
                    oneFaceGroup = TrileBoundingBoxes[i].AddFace(Vector3.One, Vector3.Backward * 0.5f, FaceOrientation.Front, new Color(128, 255, 255, 128), true);
                }
            }
            return TrileBoundingBoxes;
        }
        protected override void RefreshLevelList()
        {
            invisibleTriles.Clear();
            foreach (var trilePos in LevelManager.Triles.Keys)
            {
                var trileInstance = LevelManager.Triles[trilePos];
                var trile = trileInstance.Trile;
                if (trile.ActorSettings.Type == ActorType.Crystal)
                {
                    invisibleTriles[trilePos] = InvisibleType.Crystal;
                }
                if (trile.ActorSettings.Type == ActorType.LightningPlatform)
                {
                    invisibleTriles[trilePos] = InvisibleType.Lightning;
                }
                if (trile.SeeThrough && trile.Faces.Values.Count(c => c == CollisionType.None) == 3)
                {
                    invisibleTriles[trilePos] = InvisibleType.OneFace;
                }
            }
        }


        public override void DrawLevel(GameTime gameTime)
        {
            if (!WireframesEnabled || GameState.Loading || LevelManager.Name == null) return;

            DrawingTools.GraphicsDevice.PrepareStencilWrite(StencilMask.Level);

            foreach (var trilePos in invisibleTriles.Keys)
            {

                var type = invisibleTriles[trilePos];
                var trilebb = TrileBoundingBoxes[(int)type];
                trilebb.Position = trilePos.AsVector + Vector3.One * 0.5f;

                if (type == InvisibleType.OneFace)
                {
                    var trile = LevelManager.Triles[trilePos];
                    trilebb.Rotation = Quaternion.CreateFromYawPitchRoll(trile.Phi, 0, 0);
                    oneFaceGroup.Enabled = Vector3.Dot(CameraManager.View.Forward, trilebb.WorldMatrix.Forward) < 0.0f;
                }
                trilebb.Draw();
            }

            DrawingTools.GraphicsDevice.PrepareStencilWrite(StencilMask.None);
        }




        class InvisibleTrilesDrawToggleCommand : WireframesDrawToggleCommand
        {
            protected override string WhatFor => "hiddentriles";
            protected override string HelpWhatFor => "most of invisible triles";
            protected override string ExecuteWhatFor => "Invisible triles";
            public override bool WireframesEnabled
            {
                get => Instance.WireframesEnabled;
                set => Instance.WireframesEnabled = value;
            }
            public override Mesh[] BoundingBoxes => Instance.BoundingBoxes;
        }
    }
}
