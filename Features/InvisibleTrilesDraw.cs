using FezEngine;
using FezEngine.Effects;
using FezEngine.Services;
using FezEngine.Structure;
using FezEngine.Structure.Geometry;
using FezEngine.Tools;
using FezGame.Services;
using FEZUG.Features.Console;
using FEZUG.Helpers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FEZUG.Features
{
    internal class InvisibleTrilesDraw : IFezugFeature
    {
        private enum InvisibleType
        {
            OneFace,
            Lightning,
            Crystal
        }

        private Group oneFaceGroup;
        private Dictionary<TrileEmplacement, InvisibleType> invisibleTriles;

        private Mesh[] TrileBoundingBoxes;

        public static bool WireframesEnabled;

        [ServiceDependency]
        public ILevelMaterializer LevelMaterializer { get; set; }

        [ServiceDependency]
        public IGameLevelManager LevelManager { private get; set; }

        [ServiceDependency]
        public IGameStateManager GameState { get; set; }

        [ServiceDependency]
        public IDefaultCameraManager CameraManager { private get; set; }

        public void Initialize()
        {
            DrawActionScheduler.Schedule(delegate
            {
                invisibleTriles = new Dictionary<TrileEmplacement, InvisibleType>();
                LevelManager.LevelChanged += RefreshTrileList;

                TrileBoundingBoxes = new Mesh[3];

                var effect = new DefaultEffect.LitVertexColored
                {
                    Specular = true,
                    Emissive = 1.0f,
                    AlphaIsEmissive = true
                };

                Color[] trileColors = new Color[]
                {
                Color.Gray,
                Color.White,
                Color.Magenta
                };

                for (var i = 0; i < 3; i++)
                {
                    TrileBoundingBoxes[i] = new Mesh
                    {
                        DepthWrites = false,
                        Blending = BlendingMode.Alphablending,
                        Culling = CullMode.CullClockwiseFace
                    };

                    TrileBoundingBoxes[i].Effect = effect;

                    Color c = trileColors[i];
                    if (i == 0)
                    {
                        oneFaceGroup = TrileBoundingBoxes[i].AddFace(Vector3.One, Vector3.Backward * 0.5f, FaceOrientation.Front, new Color(128, 255, 255, 128), true);
                    }
                    TrileBoundingBoxes[i].AddWireframeBox(Vector3.One, Vector3.Zero, new Color(c.R, c.G, c.B, (i == 0) ? 32 : 255), true);
                    TrileBoundingBoxes[i].AddColoredBox(Vector3.One, Vector3.Zero, new Color(c.R, c.G, c.B, 32), true);
                }
            });
        }

        public void Update(GameTime gameTime) { }

        public void RefreshTrileList()
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
                if(trile.SeeThrough && trile.Faces.Values.Count(c => c == CollisionType.None) == 3)
                {
                    invisibleTriles[trilePos] = InvisibleType.OneFace;
                }
            }
        }


        public void DrawLevel(GameTime gameTime)
        {
            if (!WireframesEnabled || GameState.Loading || LevelManager.Name == null) return;

            DrawingTools.GraphicsDevice.PrepareStencilWrite(StencilMask.Level);

            foreach (var trilePos in invisibleTriles.Keys)
            {

                var type = invisibleTriles[trilePos];
                var trilebb = TrileBoundingBoxes[(int)type];
                trilebb.Position = trilePos.AsVector + Vector3.One * 0.5f;

                if(type == InvisibleType.OneFace)
                {
                    var trile = LevelManager.Triles[trilePos];
                    trilebb.Rotation = Quaternion.CreateFromYawPitchRoll(trile.Phi, 0, 0);
                    oneFaceGroup.Enabled = Vector3.Dot(CameraManager.View.Forward, trilebb.WorldMatrix.Forward) < 0.0f;
                }
                trilebb.Draw();
            }

            DrawingTools.GraphicsDevice.PrepareStencilWrite(StencilMask.None);
        }

        public void DrawHUD(GameTime gameTime)
        {

        }




        class InvisibleTrilesDrawToggleCommand : IFezugCommand
        {
            public string Name => "hiddentrileswireframe";

            public string HelpText => "hiddentrileswireframe [on/off] - draws wireframe for most of invisible triles";

            public List<string> Autocomplete(string[] args)
            {
                return new string[] { "on", "off" }.Where(s => s.StartsWith(args[0])).ToList();
            }

            public bool Execute(string[] args)
            {
                if (args.Length != 1)
                {
                    FezugConsole.Print($"Incorrect number of parameters: '{args.Length}'", FezugConsole.OutputType.Warning);
                    return false;
                }

                if(args[0] != "on" && args[0] != "off")
                {
                    FezugConsole.Print($"Invalid argument: '{args[0]}'", FezugConsole.OutputType.Warning);
                    return false;
                }

                WireframesEnabled = args[0] == "on";
                FezugConsole.Print($"Invisible triles wireframes have been {(WireframesEnabled ? "enabled" : "disabled")}.");
                return true;
            }
        }
    }
}
