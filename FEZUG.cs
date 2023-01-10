using Common;
using FezEngine.Components;
using FezEngine.Tools;
using FezGame;
using FezGame.Components;
using FezGame.Services;
using FEZUG.Features;
using FEZUG.Features.Console;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace FEZUG
{
    public class Fezug : DrawableGameComponent
    {
        public static string Version = "v0.1.0";

        public List<IFezugFeature> Features { get; private set; }

        public static Fezug Instance;

        [ServiceDependency]
        public IGameStateManager GameState { private get; set; }

        [ServiceDependency]
        public IGameLevelManager LevelManager { private get; set; }

        public Fezug(Game game) : base(game)
        {
            ServiceHelper.InjectServices(this);

            Instance = this;
            Enabled = true;
            Visible = true;
            DrawOrder = 99999;

            Initialize();
        }

        public override void Initialize()
        {
            base.Initialize();

            DrawingTools.Init();

            Features = new List<IFezugFeature>();
            foreach (Type type in Assembly.GetExecutingAssembly().GetTypes()
            .Where(t => t.IsClass && typeof(IFezugFeature).IsAssignableFrom(t)))
            {
                IFezugFeature feature = (IFezugFeature)Activator.CreateInstance(type);
                ServiceHelper.InjectServices(feature);
                Features.Add(feature);
            }


            var screenField = typeof(Intro).GetField("screen", BindingFlags.NonPublic | BindingFlags.Instance);
            var phaseField = typeof(Intro).GetField("phase", BindingFlags.NonPublic | BindingFlags.Instance);

            // skip to FEZ logo whenever possible
            Waiters.Wait(delegate {
                return (bool) typeof(Intro).GetField("PreloadComplete", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
            }, delegate{
                screenField.SetValue(Intro.Instance, 8);
                phaseField.SetValue(Intro.Instance, 0);

                var sTrixelOut = typeof(Intro).GetField("sTrixelOut", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(Intro.Instance);
                typeof(Intro).GetField("sTitleBassHit", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(Intro.Instance, sTrixelOut);
            });

            // make fez logo faster
            var FezLogo = (FezLogo)typeof(Intro).GetField("FezLogo", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(Intro.Instance);
            Waiters.Wait(delegate {
                return FezLogo.SinceStarted > 0;
            }, delegate {
                screenField.SetValue(Intro.Instance, 10);
                phaseField.SetValue(Intro.Instance, 0);

                Waiters.DoUntil(delegate
                {
                    return FezLogo.IsFullscreen;
                }, delegate (float delta)
                {
                    FezLogo.SinceStarted += delta * 6.0f;
                });
            });

            // get rid of dot transitions - they're pointless
            LevelManager.LevelChanged += delegate
            {
                GameState.DotLoading = false;
            };
        }

        public override void Update(GameTime gameTime)
        {
            foreach (var feature in Features)
            {
                feature.Update(gameTime);
            }
        }

        public override void Draw(GameTime gameTime)
        {

            DrawingTools.BeginBatch();

            foreach(var feature in Features)
            {
                feature.Draw(gameTime);
            }

            DrawingTools.EndBatch();
        }
    }
}
