using FezEngine;
using FezEngine.Components;
using FezEngine.Effects;
using FezEngine.Services;
using FezEngine.Structure;
using FezEngine.Structure.Geometry;
using FezEngine.Tools;
using FezGame.Components;
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
    internal class SpinningCubeHitboxDraw : IFezugFeature
    {

        private Mesh TrileBoundingBox;

        public static bool WireframesEnabled;

        [ServiceDependency]
        public IGameLevelManager LevelManager { private get; set; }

        [ServiceDependency]
        public IGameStateManager GameState { get; set; }

        Func<List<TrileInstance>> GetSpinningTreasures = null;
        public void Initialize()
        {
            System.Reflection.FieldInfo spinTreasureField = typeof(FezGame.Fez).Assembly.GetType("FezGame.Components.SpinningTreasuresHost").GetField("TrackedTreasures", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Func<SpinningTreasuresHost> getSpinningTreasuresHost = () => (SpinningTreasuresHost)ServiceHelper.Game.Components.FirstOrDefault(c => typeof(SpinningTreasuresHost).Equals(c.GetType()));
            Waiters.Wait(() => getSpinningTreasuresHost() != null, () =>
            {
                SpinningTreasuresHost spinningTreasureHost = getSpinningTreasuresHost();
                GetSpinningTreasures = () => (List<TrileInstance>)spinTreasureField.GetValue(spinningTreasureHost);
            });

            DrawActionScheduler.Schedule(delegate
            {
                var effect = new DefaultEffect.LitVertexColored
                {
                    Specular = true,
                    Emissive = 1.0f,
                    AlphaIsEmissive = true
                };

                Color trileColor = Color.Gold;

                TrileBoundingBox = new Mesh
                {
                    DepthWrites = false,
                    Blending = BlendingMode.Alphablending,
                    Culling = CullMode.CullClockwiseFace,
                    Effect = effect
                };

                Color c = trileColor;
                TrileBoundingBox.AddWireframeBox(Vector3.One, Vector3.Zero, new Color(c.R, c.G, c.B, 32), true);
                TrileBoundingBox.AddColoredBox(Vector3.One, Vector3.Zero, new Color(c.R, c.G, c.B, 32), true);
                
            });
        }

        public void Update(GameTime gameTime) { }

        public void DrawLevel(GameTime gameTime)
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

        public void DrawHUD(GameTime gameTime)
        {

        }




        class InvisibleTrilesDrawToggleCommand : IFezugCommand
        {
            public string Name => "cubehitboxes";

            public string HelpText => "cubehitboxes [on/off] - draws wireframe for collectable cubes";

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
                FezugConsole.Print($"cube hitbox wireframes have been {(WireframesEnabled ? "enabled" : "disabled")}.");
                return true;
            }
        }
    }
}
