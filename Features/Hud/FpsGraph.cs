using FezEngine.Components;
using FezEngine.Services;
using FezEngine.Tools;
using FezGame.Services;
using FEZUG.Features.Console;
using FEZUG.Helpers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FEZUG.Features.Hud
{
    public class FpsGraph : IFezugFeature
    {
        private readonly Dictionary<string, (FezugVariable fezugVar, Color lineColor, List<double> pastVals)> graphVars = [];
        private FezugVariable hud_graph_hide, graph_maxcount, graph_interval;

        private HudPositioner Positioner;

        [ServiceDependency]
        public IPlayerManager PlayerManager { private get; set; }
        [ServiceDependency]
        public IInputManager InputManager { private get; set; }

        [ServiceDependency]
        public IGameLevelManager LevelManager { private get; set; }

        [ServiceDependency]
        public IGameCameraManager CameraManager { private get; set; }

        [ServiceDependency]
        public ITimeManager TimeManager { private get; set; }

        private void CreateGraphVariable(string id, string name, string desc, Color lineColor)
        {
            graphVars.Add(id, (new FezugVariable(name, $"If set, enables {desc} graph hud.", "0")
            {
                SaveOnChange = true,
                Min = 0,
                Max = 1
            }, lineColor, []));
        }

        private void PushCircular<T>(List<T> arr, T val, int maxSize)
        {
            arr.Add(val);
            while (arr.Count > maxSize)
            {
                arr.RemoveAt(0);
            }
        }

        public void Initialize()
        {
            CreateGraphVariable("ups", "graph_ups", "updates per second", Color.Cyan);
            CreateGraphVariable("fps", "graph_fps", "frames per second", Color.Orange);

            graph_maxcount = new FezugVariable("graph_maxcount", $"Sets the maximum number of entries on the FPS graph.", "100")
            {
                SaveOnChange = true,
                Min = 2,
                Max = 1000
            };
            graph_interval = new FezugVariable("graph_interval", $"Sets the logging inverval for graph data, in milliseconds.", "1000")
            {
                SaveOnChange = true,
                Min = 10,
                Max = 60 * 1000 // 1 minute
            };
            hud_graph_hide = new FezugVariable("hud_hide_graph", "If set, hides FPS graph entirely when console is not opened.", "0")
            {
                SaveOnChange = true,
                Min = 0,
                Max = 1,
            };

            Positioner = new HudPositioner("graph", "fps graph", 1.0f, 0.0f);
        }

        private ulong _updatesDone = 0, _framesRendered = 0, _ups = 0, _fps = 0;
        private DateTime _lastTime = DateTime.Now;

        public void Update(GameTime gameTime)
        {
            _updatesDone++;
        }

        public void DrawLevel(GameTime gameTime) { }

        public int MaxBufferSize => graph_maxcount.ValueInt;
        public float GraphUpdateIntervalSeconds => graph_interval.ValueFloat / 1000;
        public void DrawHUD(GameTime gameTime)
        {
            _framesRendered++;
            float intervalSeconds = GraphUpdateIntervalSeconds;
            double elapsedSeconds = (DateTime.Now - _lastTime).TotalSeconds;
            if (elapsedSeconds >= intervalSeconds)
            {
                // one second has elapsed
                _fps = _framesRendered;
                _framesRendered = 0;
                _ups = _updatesDone;
                _updatesDone = 0;
                PushCircular(graphVars["fps"].pastVals, _fps / elapsedSeconds, MaxBufferSize);
                PushCircular(graphVars["ups"].pastVals, _ups / elapsedSeconds, MaxBufferSize);
                _lastTime = DateTime.Now;
            }

            if (hud_graph_hide.ValueBool)
            {
                var console = Fezug.GetFeature<FezugConsole>();
                if (!console.Handler.Enabled) return;
            }
            if (graphVars.Values.Select(v => v.fezugVar.ValueBool).All(b => !b))
            {
                return;
            }

            var varsForDrawing = graphVars.Where(v => v.Value.fezugVar.ValueBool && v.Value.pastVals.Count > 1);
            if (!varsForDrawing.Any())
            {
                return;
            }

            int enabledCount = varsForDrawing.Count();
            float padX = 10.0f;
            Viewport viewport = DrawingTools.GraphicsDevice.Viewport;
            float width = viewport.Width * 0.3f + padX * 2;
            float height = viewport.Height * 0.3f + 15;
            float padding = 5f;
            int margin = 5;
            var position = Positioner.GetPosition(width + 2 * margin, height + 2 * margin);

            DrawingTools.DrawRect(new Rectangle((int)(position.X + margin), (int)(position.Y + margin), (int)width, (int)height), new Color(10, 10, 10, 220));

            var xOff = position.X + margin + padding;
            var yOff = position.Y + margin + padding;
            Vector2 boxTopLeft = new(xOff, yOff);
            width -= padding;
            height -= padding;
            var lineThickness = 2;// Math.Max(1, (int)Math.Ceiling(Math.Min(viewport.Width, viewport.Height) * 0.003f));
            var avgLineColorAdjust = 0.5f;
            float textHeight = DrawingTools.DefaultFont.MeasureString("A").Y * DrawingTools.DefaultFontSize;

            int displayTextIndex = 0;
            foreach (var pair in varsForDrawing)
            {
                var (fezugVar, lineColor, pastVals) = pair.Value;
                if (fezugVar.ValueBool && pastVals.Count > 1)
                {
                    var avgLineColor = Color.Multiply(lineColor, avgLineColorAdjust);
                    avgLineColor.A /= 2;
                    float max = (float)pastVals.Max();
                    var xScale = (width - padding) / (pastVals.Count - 1);
                    var yScale = (height - padding) / max;
                    Vector2 previousPoint = boxTopLeft + new Vector2(0, yScale * (float)pastVals[0]);
                    for (int i=1; i<pastVals.Count; ++i)
                    {
                        //draw line in box
                        Vector2 nextPoint = boxTopLeft + new Vector2(xScale * i, (float)(yScale * pastVals[i]));
                        DrawingTools.DrawLineSegment(previousPoint, nextPoint, lineColor, lineThickness);
                        //draw line joints / nodes
                        //DrawingTools.DrawRect(new Rectangle((int)point1.X + lineThickness, (int)point1.Y, lineThickness, lineThickness), lineColor);
                        previousPoint = nextPoint;
                    }
                    float avg = (float)pastVals.Average();
                    float avgPosY = yOff + avg * yScale;
                    //draw average
                    Vector2 avgTextPos = new Vector2(xOff, yOff + textHeight * (enabledCount - 1 - displayTextIndex));
                    DrawingTools.DrawLineSegment(new(xOff, avgPosY), new(xOff + width, avgPosY), avgLineColor, lineThickness);
                    DrawingTools.DrawText(pair.Key.ToUpper() + " (avg): " + avg, avgTextPos, avgLineColor);
                    ++displayTextIndex;
                }
            }
        }
    }
}
