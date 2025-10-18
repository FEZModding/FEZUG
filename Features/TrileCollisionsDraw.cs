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
using System.Linq;
using System.Security.Cryptography;

namespace FEZUG.Features
{
    internal class TrileCollisionsDraw : WireframeDraw
    {
        public static TrileCollisionsDraw Instance;

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
        private readonly Dictionary<FaceOrientation, Quaternion> FaceOrientationToQuaternion = ((IEnumerable<FaceOrientation>)Enum.GetValues(typeof(FaceOrientation))).Select(face => {
            return (key: face, value: Quaternion.CreateFromAxisAngle(face.AsVector(), 0));
        }).ToDictionary(p => p.key, p => p.value);

        protected override Mesh[] RefreshBoundingBoxMeshs()
        {
            var i = 0;
            foreach (var color in colors)
            {
                CollisionMeshes[color.Key] = CreateHitboxMesh(color.Value);

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
            }
            foreach (var pair in invisibleTriles)
            {
                var trilePos = pair.Key;
                var type = invisibleTriles[trilePos];
                if (pair.Value.TryGetValue(cameraViewpoint, out var collisionType))
                {
                    var trilebb = CollisionMeshes[collisionType];
                    trilebb.Position = trilePos.AsVector + Vector3.One * 0.5f;
                    trilebb.Rotation = FaceOrientationToQuaternion[cameraViewpoint];
                    trilebb.Draw();
                }
                else
                {
                    foreach (var faces in pair.Value)
                    {
                        var trilebb = CollisionMeshes[faces.Value];
                        trilebb.Position = trilePos.AsVector + Vector3.One * 0.5f;
                        trilebb.Draw();
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
        }
    }
}
