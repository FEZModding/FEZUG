using FezEngine.Components;
using FezEngine.Services;
using FezEngine.Tools;
using FezGame.Services;
using FEZUG.Features.Console;
using FEZUG.Helpers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace FEZUG.Features.Hud
{
    public class FpsGraph : IFezugFeature
    {
        private Dictionary<string, (FezugVariable var, Color lineColor, List<double> pastVals)> graphVars = [];
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

        public void Initialize()
        {
            void CreateGraphVariable(string name, string desc, Color lineColor)
            {
                graphVars.Add(name, (new FezugVariable(name, $"If set, enables {desc} graph hud.", "0")
                {
                    SaveOnChange = true,
                    Min = 0,
                    Max = 1
                }, lineColor, []));
            }

            CreateGraphVariable("graph_ups", "updates per second", Color.Cyan);
            CreateGraphVariable("graph_fps", "frames per second", Color.Orange);

            graph_maxcount = new FezugVariable("graph_maxcount", $"Sets the maximum number of entries on the FPS graph.", "100")
            {
                SaveOnChange = true,
                Min = 2,
                Max = 1000
            };
            graph_interval = new FezugVariable("graph_interval", $"Sets the logging inverval for graph data, in 60ths of a second.", "60")
            {
                SaveOnChange = true,
                Min = 5,
                Max = 60 * 60 // 1 minute
            };
            //graph_interval.OnChanged += ()=>
            //{
            //    _framesRendered = 0;
            //    _updatesDone = 0;
            //};
            hud_graph_hide = new FezugVariable("hud_hide_graph", "If set, hides FPS graph entirely when console is not opened.", "0")
            {
                SaveOnChange = true,
                Min = 0,
                Max = 1,
            };

            Positioner = new HudPositioner("graph", "fps graph", 1.0f, 0.0f);
        }

        private void DrawText(string text, Vector2 pos)
        {
            DrawingTools.DrawText(text, pos, Color.White);
        }

        private ulong _updatesDone = 0, _framesRendered = 0, _ups = 0, _fps = 0;
        private DateTime _lastTime = DateTime.Now;

        public void Update(GameTime gameTime)
        {
            _updatesDone++;
        }

        public void DrawLevel(GameTime gameTime) { }

        public int MaxBufferSize => graph_maxcount.ValueInt;
        public float GraphUpdateIntervalSeconds => graph_interval.ValueFloat / 60f;
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
                static void PushCircular<T>(List<T> arr, T val, int maxSize)
                {
                    arr.Add(val);
                    //TODO use arr.Skip instead of this while loop
                    while (arr.Count > maxSize)
                    {
                        arr.RemoveAt(0);
                    }
                }
                PushCircular(graphVars["graph_fps"].pastVals, _fps / elapsedSeconds, MaxBufferSize);
                PushCircular(graphVars["graph_ups"].pastVals, _ups / elapsedSeconds, MaxBufferSize);
                _lastTime = DateTime.Now;
            }

            if (hud_graph_hide.ValueBool)
            {
                var console = Fezug.GetFeature<FezugConsole>();
                if (!console.Handler.Enabled) return;
            }
            if(graphVars.Values.Select(v=>v.var.ValueBool).All(b=>!b))
            {
                return;
            }

            float padX = 10.0f;
            Viewport viewport = DrawingTools.GraphicsDevice.Viewport;
            float width = viewport.Width * 0.3f + padX * 2;
            float height = viewport.Height * 0.3f + 15;
            int margin = 5;
            var position = Positioner.GetPosition(width + 2 * margin, height + 2 * margin);

            DrawingTools.DrawRect(new Rectangle((int)(position.X + margin), (int)(position.Y + margin), (int)width, (int)height), new Color(10, 10, 10, 220));

            var xOff = position.X + margin;
            var yOff = position.Y + margin;
            var lineThickness = 2;// Math.Max(1, (int)Math.Ceiling(Math.Min(viewport.Width, viewport.Height) * 0.003f));

            foreach (var (var, lineColor, pastVals) in graphVars.Values)
            {
                if (var.ValueBool && pastVals.Count > 1)
                {
                    float max = (float)pastVals.Max();
                    var xScale = width / (pastVals.Count - 1);
                    var yScale = height / max;
                    for (int i=1; i<pastVals.Count; ++i)
                    {
                        var p = i - 1;
                        //draw line in box
                        Vector2 point1 = new(xOff + xScale * p, (int)(yOff + yScale * pastVals[p]));
                        Vector2 point2 = new(xOff + xScale * i, (int)(yOff + yScale * pastVals[i]));
                        DrawingTools.DrawLineSegment(point1, point2, lineColor, lineThickness);
                        //draw line joints / nodes
                        //DrawingTools.DrawRect(new Rectangle((int)point1.X + lineThickness, (int)point1.Y, lineThickness, lineThickness), lineColor);
                    }
                }
            }
        }
    }
}
