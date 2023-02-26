using Common;
using FezEngine.Components;
using FezEngine.Tools;
using FezGame;
using FezGame.Components;
using FezGame.Services;
using FEZUG.Features;
using FEZUG.Features.Console;
using FEZUG.Helpers;
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
        public static string Version = "v0.1.5";

        public List<IFezugFeature> Features { get; private set; }

        public static Fezug Instance { get; private set; }
        public static Fez Fez { get; private set; }

        public Fezug(Game game) : base(game)
        {
            Fez = (Fez)game;
            Instance = this;
            Enabled = true;
            Visible = true;
            DrawOrder = 99999;
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

            foreach (var feature in Features)
            {
                feature.Initialize();
            }
        }

        public static IFezugFeature GetFeature<T>()
        {
            return GetFeature(typeof(T));
        }

        public static IFezugFeature GetFeature(Type type)
        {
            foreach (var feature in Instance.Features)
            {
                if (feature.GetType() == type) return feature;
            }
            return null;
        }

        public override void Update(GameTime gameTime)
        {
            InputHelper.Update(gameTime);

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
                feature.DrawHUD(gameTime);
            }

            DrawingTools.EndBatch();
        }
    }

    public class FezugInGameRendering : DrawableGameComponent
    {
        public FezugInGameRendering(Game game) : base(game)
        {
            Enabled = true;
            Visible = true;
            DrawOrder = 101;
        }

        public override void Draw(GameTime gameTime)
        {
            foreach (var feature in Fezug.Instance.Features)
            {
                feature.DrawLevel(gameTime);
            }
        }
    }
}
