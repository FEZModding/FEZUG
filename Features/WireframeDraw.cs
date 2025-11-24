using FezEngine.Effects;
using FezEngine.Services;
using FezEngine.Structure;
using FezEngine.Tools;
using FezGame.Services;
using FEZUG.Features.Console;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FEZUG.Features
{
    internal abstract class WireframeDraw : IFezugFeature
    {
        protected Mesh[] BoundingBoxes = null;

        public bool WireframesEnabled;

        [ServiceDependency]
        public ILevelMaterializer LevelMaterializer { get; set; }

        [ServiceDependency]
        public IGameLevelManager LevelManager { protected get; set; }

        [ServiceDependency]
        public IPlayerManager PlayerManager { protected get; set; }

        [ServiceDependency]
        public IGameStateManager GameState { get; set; }

        [ServiceDependency]
        public IDefaultCameraManager CameraManager { protected get; set; }

        protected Mesh CreateHitboxMesh(Color c)
        {
            BaseEffect effect = new DefaultEffect.LitVertexColored
            {
                Specular = true,
                Emissive = 1.0f,
                AlphaIsEmissive = true
            };
            Mesh m = new Mesh
            {
                DepthWrites = false,
                Blending = BlendingMode.Alphablending,
                Culling = CullMode.CullClockwiseFace,
                Effect = effect
            };
            m.AddWireframeBox(Vector3.One, Vector3.Zero, new Color(c.R, c.G, c.B, 255), true);
            m.AddColoredBox(Vector3.One, Vector3.Zero, new Color(c.R, c.G, c.B, 32), true);
            return m;
        }
        protected abstract Mesh[] RefreshBoundingBoxMeshs();
        protected abstract void RefreshLevelList();
        protected virtual void PreInitialize() { }

        public void Initialize()
        {
            PreInitialize();
            DrawActionScheduler.Schedule(delegate
            {
                LevelManager.LevelChanged += RefreshLevelList;
                BoundingBoxes ??= RefreshBoundingBoxMeshs();
            });
        }

        public void Update(GameTime gameTime) { }

        public abstract void DrawLevel(GameTime gameTime);

        public void DrawHUD(GameTime gameTime)
        {

        }




        protected abstract class WireframesDrawToggleCommand : IFezugCommand
        {
            private const string onOption = "on";
            private const string offOption = "off";
            private const string xrayOption = "xray";

            protected bool AllowXray = false;
            private string[] Options
            {
                get
                {
                    if (AllowXray)
                    {
                        return [onOption, offOption, xrayOption, ..AdditionalChoices.Keys];
                    }
                    return [onOption, offOption, ..AdditionalChoices.Keys];
                }
            }
            protected virtual Dictionary<string, Func<string[], string>> AdditionalChoices => [];

            protected abstract string WhatFor { get; }
            protected virtual string HelpWhatFor => WhatFor;
            protected virtual string ExecuteWhatFor => WhatFor;
            public string Name => WhatFor.ToLower() + "wireframe";

            public string HelpText => Name + " [" + string.Join("/", Options) + "] - draws wireframe for " + HelpWhatFor;

            public abstract bool WireframesEnabled { get; set; }
            public abstract Mesh[] BoundingBoxes { get; }

            public List<string> Autocomplete(string[] args)
            {
                return [.. Options.Where(s => s.StartsWith(args[0]))];
            }

            public bool Execute(string[] args)
            {
                if (args.Length != 1)
                {
                    FezugConsole.Print($"Incorrect number of parameters: '{args.Length}'", FezugConsole.OutputType.Warning);
                    return false;
                }

                if (!Options.Contains(args[0]))
                {
                    FezugConsole.Print($"Invalid argument: '{args[0]}'", FezugConsole.OutputType.Warning);
                    return false;
                }

                if (AdditionalChoices.TryGetValue(args[0], out var value))
                {
                    FezugConsole.Print(value(args));
                    return true;
                }

                bool xray = args[0] == xrayOption;
                WireframesEnabled = xray || args[0] == onOption;
                foreach (var vol in BoundingBoxes)
                {
                    vol.AlwaysOnTop = xray;
                }
                FezugConsole.Print($"{ExecuteWhatFor} wireframes have been {(WireframesEnabled ? "enabled" + (xray ? " in xray mode" : "") : "disabled")}.");
                return true;
            }
        }
    }
}
