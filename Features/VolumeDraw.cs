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
    internal class VolumeDraw : IFezugFeature
    {
        private enum VolumeType
        {
            BlackHole,
            PointOfInterest,
            CodeZone,
            Shortcut,
            Door,
            Other
        }

        private Dictionary<int, VolumeType> volumes;

        private Mesh[] VolumeBoundingBoxes;

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
                volumes = new Dictionary<int, VolumeType>();
                LevelManager.LevelChanged += RefreshVolumeList;

                VolumeBoundingBoxes = new Mesh[3];

                var effect = new DefaultEffect.LitVertexColored
                {
                    Specular = true,
                    Emissive = 1.0f,
                    AlphaIsEmissive = true
                };

                Color[] volColors = new Color[]
                {
                Color.Gray,
                Color.White,
                Color.Magenta
                };

                for (var i = 0; i < 3; i++)
                {
                    VolumeBoundingBoxes[i] = new Mesh
                    {
                        DepthWrites = false,
                        Blending = BlendingMode.Alphablending,
                        Culling = CullMode.CullClockwiseFace
                    };

                    VolumeBoundingBoxes[i].Effect = effect;

                    Color c = volColors[i];
                    VolumeBoundingBoxes[i].AddWireframeBox(Vector3.One, Vector3.Zero, new Color(c.R, c.G, c.B, (i == 0) ? 32 : 255), true);
                    VolumeBoundingBoxes[i].AddColoredBox(Vector3.One, Vector3.Zero, new Color(c.R, c.G, c.B, 32), true);
                }
            });
        }

        public void Update(GameTime gameTime) { }

        public void RefreshVolumeList()
        {
            volumes.Clear();
            if (LevelManager.Volumes != null)
            {
                foreach (var volID in LevelManager.Volumes.Keys)
                {
                    var volume = LevelManager.Volumes[volID];
                    volumes[volID] = VolumeType.Other;
                    if (volume.ActorSettings != null)
                    {
                        if (volume.ActorSettings.IsBlackHole)
                        {
                            volumes[volID] = VolumeType.BlackHole;
                        }
                        else if (volume.ActorSettings.CodePattern != null && volume.ActorSettings.CodePattern.Length > 0)
                        {
                            volumes[volID] = VolumeType.CodeZone;
                        }
                        else if (volume.ActorSettings.IsSecretPassage)
                        {
                            volumes[volID] = VolumeType.Shortcut;
                        }
                        else if (volume.ActorSettings.IsPointOfInterest)
                        {
                            volumes[volID] = VolumeType.PointOfInterest;
                        }
                    }
                    else
                    {
                        var myvolscripts = LevelManager.Scripts.Values.Where(script => script.Triggers.Any(trigger => trigger.Object.Type.Equals("Volume") && trigger.Object.Identifier == volID));
                        if (myvolscripts.Count() > 0)
                        {
                            if (myvolscripts.Any(script => script.Actions.Any(action => action.Operation.Contains("ChangeLevel"))))
                            {
                                volumes[volID] = VolumeType.Door;
                            }
                        }
                    }
                }
            }
        }


        public void DrawLevel(GameTime gameTime)
        {
            if (!WireframesEnabled || GameState.Loading || LevelManager.Name == null) return;

            DrawingTools.GraphicsDevice.PrepareStencilWrite(StencilMask.Level);

            foreach (var volID in volumes.Keys)
            {

                var type = volumes[volID];
                var volbb = VolumeBoundingBoxes[(int)type%3];
                if (LevelManager.Volumes.TryGetValue(volID, out var vol))
                {
                    var volPos = vol.From;
                    var volSize = vol.From - vol.To;
                    volbb.Position = volPos - volSize/2;
                    volbb.Scale = volSize;
                    volbb.Draw();
                }
            }

            DrawingTools.GraphicsDevice.PrepareStencilWrite(StencilMask.None);
        }

        public void DrawHUD(GameTime gameTime)
        {

        }




        class VolumesDrawToggleCommand : IFezugCommand
        {
            public string Name => "volumeswireframe";

            public string HelpText => "volumeswireframe [on/off] - draws wireframe for volumes (Note: not tested)";

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
                FezugConsole.Print($"Volume wireframes have been {(WireframesEnabled ? "enabled" : "disabled")}.");
                return true;
            }
        }
    }
}
