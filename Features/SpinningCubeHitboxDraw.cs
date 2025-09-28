using FezEngine.Components;
using FezEngine.Effects;
using FezEngine.Structure;
using FezEngine.Tools;
using FezGame.Components;
using FezGame.Services;
using FEZUG.Features.Console;
using FEZUG.Helpers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FEZUG.Features
{
    internal class SpinningCubeHitboxDraw : WireframeDraw
    {
        public static SpinningCubeHitboxDraw Instance;

        public SpinningCubeHitboxDraw() : base()
        {
            Instance = this;
        }

        private Mesh TrileBoundingBox;

        Func<List<TrileInstance>> GetSpinningTreasures = null;

        protected override Mesh[] RefreshBoundingBoxMeshs()
        {
            return [TrileBoundingBox = CreateHitboxMesh(Color.Gold)];
        }
        protected override void PreInitialize()
        {
            System.Reflection.FieldInfo spinTreasureField = typeof(FezGame.Fez).Assembly.GetType("FezGame.Components.SpinningTreasuresHost").GetField("TrackedTreasures", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Func<SpinningTreasuresHost> getSpinningTreasuresHost = () => (SpinningTreasuresHost)ServiceHelper.Game.Components.FirstOrDefault(c => typeof(SpinningTreasuresHost).Equals(c.GetType()));
            Waiters.Wait(() => getSpinningTreasuresHost() != null, () =>
            {
                SpinningTreasuresHost spinningTreasureHost = getSpinningTreasuresHost();
                GetSpinningTreasures = () => (List<TrileInstance>)spinTreasureField.GetValue(spinningTreasureHost);
            });
        }

        public override void DrawLevel(GameTime gameTime)
        {
            if (!WireframesEnabled || GameState.Loading || LevelManager.Name == null) return;

            DrawingTools.GraphicsDevice.PrepareStencilWrite(StencilMask.Level);

            GetSpinningTreasures?.Invoke()?.ForEach(trile =>
            {
                var trilebb = TrileBoundingBox;
                trilebb.Position = trile.Position + Vector3.One * 0.5f;
                trilebb.Draw();
            });

            DrawingTools.GraphicsDevice.PrepareStencilWrite(StencilMask.None);
        }

        protected override void RefreshLevelList() { }




        class CubeHitboxDrawToggleCommand : WireframesDrawToggleCommand
        {
            protected override string WhatFor => "cubehitboxes";
            protected override string HelpWhatFor => "collectable cubes";
            protected override string ExecuteWhatFor => "cube hitbox wireframes";
            public override bool WireframesEnabled
            {
                get => Instance.WireframesEnabled;
                set => Instance.WireframesEnabled = value;
            }
            public override Mesh[] BoundingBoxes => Instance.BoundingBoxes;
        }
    }
}
