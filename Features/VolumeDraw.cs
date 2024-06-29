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

        private static Mesh[] VolumeBoundingBoxes = null;

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

                if (VolumeBoundingBoxes == null)
                {
                    Color[] volColors = new Color[]
                    {
                        Color.Black,
                        Color.Gold,
                        Color.Blue,
                        Color.Gray,
                        Color.Lime,
                        Color.Purple
                    };
                    int VolTypeCount = volColors.Length;
                    VolumeBoundingBoxes = new Mesh[VolTypeCount];

                    var effect = new DefaultEffect.LitVertexColored
                    {
                        Specular = true,
                        Emissive = 1.0f,
                        AlphaIsEmissive = true
                    };

                    for (var i = 0; i < VolTypeCount; i++)
                    {
                        VolumeBoundingBoxes[i] = new Mesh
                        {
                            DepthWrites = false,
                            Blending = BlendingMode.Alphablending,
                            Culling = CullMode.CullClockwiseFace,
                        };

                        VolumeBoundingBoxes[i].Effect = effect;

                        Color c = volColors[i];
                        VolumeBoundingBoxes[i].AddWireframeBox(Vector3.One, Vector3.Zero, new Color(c.R, c.G, c.B, 255), true);
                        VolumeBoundingBoxes[i].AddColoredBox(Vector3.One, Vector3.Zero, new Color(c.R, c.G, c.B, 32), true);
                    }
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
                            if (myvolscripts.Any(script =>
                                    //Should match AllowPipeChangeLevel, ChangeLevel, ChangeToFarAwayLevel, ChangeLevelToVolume, ExploChangeLevel and maybe ReturnToLastLevel
                                    script.Actions.Any(action =>
                                        (action.Operation.Contains("Change") || action.Operation.Contains("Return"))
                                        && action.Operation.Contains("Level")
                                    )
                                ))
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
                var volbb = VolumeBoundingBoxes[(int)type % VolumeBoundingBoxes.Length];
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

            public string HelpText => "volumeswireframe [on/off/xray] - draws wireframe for volumes";

            public List<string> Autocomplete(string[] args)
            {
                return new string[] { "on", "off", "xray" }.Where(s => s.StartsWith(args[0])).ToList();
            }

            public bool Execute(string[] args)
            {
                if (args.Length != 1)
                {
                    FezugConsole.Print($"Incorrect number of parameters: '{args.Length}'", FezugConsole.OutputType.Warning);
                    return false;
                }

                if(args[0] != "on" && args[0] != "off" && args[0] != "xray")
                {
                    FezugConsole.Print($"Invalid argument: '{args[0]}'", FezugConsole.OutputType.Warning);
                    return false;
                }

                bool xray = args[0] == "xray";
                WireframesEnabled = xray || args[0] == "on";
                foreach(var vol in VolumeBoundingBoxes) { 
                    vol.AlwaysOnTop = xray;
                }
                FezugConsole.Print($"Volume wireframes have been {(WireframesEnabled ? "enabled" + (xray ? " in xray mode" : "") : "disabled")}.");
                return true;
            }
        }
    }
}
