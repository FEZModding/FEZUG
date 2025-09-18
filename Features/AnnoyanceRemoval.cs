using FezEngine.Components;
using FezEngine.Tools;
using FezGame;
using FezGame.Components;
using FezGame.Services;
using FezGame.Structure;
using FEZUG.Features.Console;
using Microsoft.Xna.Framework;
using MonoMod.RuntimeDetour;
using System.Reflection;

namespace FEZUG.Features
{
    internal class AnnoyanceRemoval : IFezugFeature
    {
        private Type IntroPanDownType;
        private List<string> DotLoadLevelsReference;
        private List<string> OriginalDotLoadLevels;
        private IDetour LevelManagerChangeLevelDetour;

        private FezugVariable SkipDotLoadingVariable;

        [ServiceDependency]
        public IPlayerManager PlayerManager { private get; set; }

        [ServiceDependency]
        public IGameStateManager GameState { private get; set; }

        [ServiceDependency]
        public IGameLevelManager LevelManager { private get; set; }

        public void Initialize()
        {
            // removing dot loading screens
            LevelManagerChangeLevelDetour = new Hook(
                typeof(GameLevelManager).GetMethod("ChangeLevel"),
                ChangeLevelHooked);
            var DotLoadLevelsField = typeof(GameLevelManager).GetField("DotLoadLevels", BindingFlags.NonPublic | BindingFlags.Instance);
            DotLoadLevelsReference = (List<string>)DotLoadLevelsField.GetValue(LevelManager);
            OriginalDotLoadLevels = new List<string>(DotLoadLevelsReference);

            SkipDotLoadingVariable = new FezugVariable("skip_dot_loading", "If set, skip the long Dot loading screens", "true")
            {
                SaveOnChange = true,
                Min = 0,
                Max = 1
            };
            SkipDotLoadingVariable.OnChanged += OnDotLoadingSkipVariableChanged;
            if (SkipDotLoadingVariable.ValueBool)
                DotLoadLevelsReference.Clear();

            var screenField = typeof(Intro).GetField("screen", BindingFlags.NonPublic | BindingFlags.Instance);
            var phaseField = typeof(Intro).GetField("phase", BindingFlags.NonPublic | BindingFlags.Instance);

            // skip to FEZ logo whenever possible
            Waiters.Wait(delegate {
                return (bool)typeof(Intro).GetField("PreloadComplete", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
            }, delegate {
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

            IntroPanDownType = Assembly.GetAssembly(typeof(Fez)).GetType("FezGame.Components.IntroPanDown");
        }

        public void Update(GameTime gameTime)
        {
            // get rid of IntroPanDown whenever it appears
            if(Intro.Instance != null)
            {
                var IntroPanDownField = typeof(Intro).GetField("IntroPanDown", BindingFlags.NonPublic | BindingFlags.Instance);
                var IntroPanDown = IntroPanDownField.GetValue(Intro.Instance);
                if (IntroPanDown != null)
                {
                    var SinceStartedField = IntroPanDownType.GetField("SinceStarted", BindingFlags.Public | BindingFlags.Instance);
                    var DistanceField = IntroPanDownType.GetField("Distance", BindingFlags.NonPublic | BindingFlags.Instance);
                    float Distance = (float)DistanceField.GetValue(IntroPanDown);
                    SinceStartedField.SetValue(IntroPanDown, Distance);
                }
            }

        }

        public void DrawHUD(GameTime gameTime)
        {
        }

        public void DrawLevel(GameTime gameTime)
        {
        }

        public void ChangeLevelHooked(Action<GameLevelManager, string> original, GameLevelManager self, string levelName)
        {
            if (SkipDotLoadingVariable.ValueBool) {
                if (PlayerManager.Action == ActionType.LesserWarp || PlayerManager.Action == ActionType.GateWarp)
                    PlayerManager.Action = ActionType.EnteringDoor;
            }
            original(self, levelName);
        }

        public void OnDotLoadingSkipVariableChanged()
        {
            if (SkipDotLoadingVariable.ValueBool)
            {
                DotLoadLevelsReference.Clear();
            }
            else
            {
                if (DotLoadLevelsReference.Count() == 0)
                    DotLoadLevelsReference.AddRange(OriginalDotLoadLevels);
            }
        }
    }
}
