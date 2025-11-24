using FezEngine;
using FezEngine.Components;
using FezEngine.Effects;
using FezEngine.Structure;
using FezEngine.Tools;
using FEZUG.Helpers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FEZUG.Features
{
    internal class TrileCollisionsDraw : WireframeDraw
    {
        public static TrileCollisionsDraw Instance;
        private bool ShowNone = true;

        public TrileCollisionsDraw() : base()
        {
            Instance = this;
        }

        private static readonly Dictionary<CollisionType, Color> colors = new(){
            {CollisionType.None, Color.Green},
            {CollisionType.AllSides, Color.Red},
            {CollisionType.TopOnly, Color.Blue},
            {CollisionType.Immaterial, Color.Lime},
            {CollisionType.TopNoStraightLedge, Color.Yellow},
        };

        private readonly Dictionary<TrileEmplacement, Dictionary<FaceOrientation, CollisionType>> invisibleTriles = [];
        private readonly Dictionary<CollisionType, Mesh> CollisionMeshes = [];

        protected override Mesh[] RefreshBoundingBoxMeshs()
        {
            var sortedFaces = Common.Util.GetValues<FaceOrientation>().OrderBy(a => (int)a);
            foreach (var color in colors.OrderBy(a => (int)a.Key))
            {
                var mesh = CollisionMeshes[color.Key] = new Mesh();
                mesh.Effect = new DefaultEffect.Textured();

                Waiters.Wait(() =>
                {
                    return DrawingTools.DefaultFont != null && DrawingTools.DefaultFontSize > 0f && DrawingTools.GraphicsDevice != null && DrawingTools.Batch != null;
                }, () =>
                {
                    string text = color.Key.ToString();
                    Vector2 textSize = DrawingTools.DefaultFont.MeasureString(text) * DrawingTools.DefaultFontSize;
                    var BackgroundColor = color.Value;

                    BackgroundColor.A = 200;
                    RenderTarget2D textTexture = new(DrawingTools.GraphicsDevice, (int)textSize.X, (int)textSize.Y, mipMap: false, DrawingTools.GraphicsDevice.PresentationParameters.BackBufferFormat, DrawingTools.GraphicsDevice.PresentationParameters.DepthStencilFormat, 0, RenderTargetUsage.PreserveContents);

                    DrawingTools.GraphicsDevice.SetRenderTarget(textTexture);
                    DrawingTools.GraphicsDevice.PrepareDraw();
                    DrawingTools.GraphicsDevice.Clear(ClearOptions.Target, BackgroundColor, 1f, 0);
                    if (Culture.IsCJK)
                    {
                        DrawingTools.Batch.BeginLinear();
                    }
                    else
                    {
                        DrawingTools.Batch.BeginPoint();
                    }
                    DrawingTools.DrawText(text, Vector2.Zero, 0, 1f, Color.Black);
                    DrawingTools.DrawText(text, Vector2.Zero + Vector2.One, 0, 1f, Color.White);
                    DrawingTools.EndBatch();
                    DrawingTools.GraphicsDevice.SetRenderTarget(null);

                    mesh.Texture = textTexture;
                    mesh.SamplerState = Culture.IsCJK ? SamplerState.AnisotropicClamp : SamplerState.PointClamp;
                    mesh.Material.Opacity = 1;
                    mesh.Texture = textTexture;
                    mesh.AlwaysOnTop = true;

                    foreach (FaceOrientation orientation in sortedFaces)
                    {
                        Group newGroup = mesh.AddFace(Vector3.One, Vector3.Zero, orientation, true);
                    }
                });
            }
            return [.. CollisionMeshes.Values];
        }
        protected override void RefreshLevelList()
        {
            invisibleTriles.Clear();
            foreach (var trile in LevelManager.Triles)
            {
                invisibleTriles[trile.Key] = trile.Value.Trile.Faces;
            }
        }


        public override void DrawLevel(GameTime gameTime)
        {
            if (!WireframesEnabled || GameState.Loading || LevelManager.Name == null) return;

            DrawingTools.GraphicsDevice.PrepareStencilWrite(StencilMask.Level);

            var cameraViewpoint = CameraManager.Viewpoint.VisibleOrientation();

            foreach (var trile in LevelManager.Triles)
            {
                Dictionary<FaceOrientation, CollisionType> faces = trile.Value.Trile.Faces;
                foreach (var face in faces)
                {
                    if(face.Value == CollisionType.None && !ShowNone)
                    {
                        continue;
                    }
                    if (CollisionMeshes.TryGetValue(face.Value, out Mesh mesh))
                    {
                        foreach (var group in mesh.Groups)
                        {
                            group.Enabled = false;
                        }
                        Group activeGroup = mesh.Groups[(int)face.Key];
                        activeGroup.Position = trile.Value.Position + Vector3.One * 0.5f + Vector3.One * 0.5f * face.Key.AsVector();
                        activeGroup.Enabled = true;
                        mesh.Draw();
                    }
                }
            }

            DrawingTools.GraphicsDevice.PrepareStencilWrite(StencilMask.None);
        }




        class TrileCollisionsDrawToggleCommand : WireframesDrawToggleCommand
        {
            protected override string WhatFor => "trilecollisions";
            protected override string HelpWhatFor => "trile collisions";
            protected override string ExecuteWhatFor => "Trile collision";
            public override bool WireframesEnabled
            {
                get => Instance.WireframesEnabled;
                set => Instance.WireframesEnabled = value;
            }
            public override Mesh[] BoundingBoxes => Instance.BoundingBoxes;

            protected override Dictionary<string, Func<string[], string>> AdditionalChoices => new()
            {
                {
                    "toggle_hide_none", (args)=>{
                        Instance.ShowNone = !Instance.ShowNone;
                        return $"Wireframes for the \"None\" CollisionType have been {(Instance.ShowNone ? "enabled" : "disabled")}.";
                    }
                } 
            };
        }
    }
}
