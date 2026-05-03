using FezEngine.Tools;
using FezGame;
using FEZUG.Features;
using FEZUG.Helpers;
using Microsoft.Xna.Framework;
using System.Reflection;

namespace FEZUG
{
    public class Fezug : DrawableGameComponent
    {
        public static string Version = "v0.1.7";

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

            Features = new();

            try {
                Type[] types = Assembly.GetExecutingAssembly().GetTypes();
                foreach (Type type in types.Where(t => t.IsClass && typeof(IFezugFeature).IsAssignableFrom(t) && !t.IsAbstract))
                {
                    IFezugFeature feature = (IFezugFeature)Activator.CreateInstance(type);
                    ServiceHelper.InjectServices(feature);
                    Features.Add(feature);
                }

                foreach (var feature in Features)
                {
                    feature.Initialize();
                }
            } catch (ReflectionTypeLoadException e) {
                string message = "Error while attempting to load types in this assembly!!! The classes in the following error messages are the ones with issues:\n";
                foreach (Exception ex in e.LoaderExceptions) {
                    message += "\n" + ex.Message;
                }
                throw new Exception(message);
            }
        }

        public static T GetFeature<T>()
        {
            return (T)GetFeature(typeof(T));
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
            foreach (var feature in Features)
            {
                feature.Update(gameTime);
            }
        }

        public override void Draw(GameTime gameTime)
        {
            // update InputHelper from draw loop to avoid dealing with timescale jank.
            InputHelper.Instance.Update(Timescaler.GetUnscaledGameTime(gameTime));
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
