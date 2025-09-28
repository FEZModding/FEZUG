using FezEngine.Effects;
using FezEngine.Structure;
using FezEngine.Tools;
using FEZUG.Helpers;
using Microsoft.Xna.Framework;

namespace FEZUG.Features
{
    internal class VolumeDraw : WireframeDraw
    {
        public static VolumeDraw Instance;

        public VolumeDraw() : base()
        {
            Instance = this;
        }

        private enum VolumeType
        {
            BlackHole,
            PointOfInterest,
            CodeZone,
            Shortcut,
            Door,
            Other
        }

        private Dictionary<int, VolumeType> volumes = [];

        private static Mesh[] VolumeBoundingBoxes = null;

        protected override Mesh[] RefreshBoundingBoxMeshs()
        {
            Color[] volColors =
            [
                Color.Black,
                Color.Gold,
                Color.Blue,
                Color.Gray,
                Color.Lime,
                Color.Purple
            ];
            int VolTypeCount = volColors.Length;
            VolumeBoundingBoxes = new Mesh[VolTypeCount];

            for (var i = 0; i < VolTypeCount; i++)
            {
                VolumeBoundingBoxes[i] = CreateHitboxMesh(volColors[i]);
            }
            return VolumeBoundingBoxes;
        }

        protected override void RefreshLevelList()
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


        public override void DrawLevel(GameTime gameTime)
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
                    volbb.Position = volPos - volSize / 2;
                    volbb.Scale = volSize;
                    volbb.Draw();
                }
            }

            DrawingTools.GraphicsDevice.PrepareStencilWrite(StencilMask.None);
        }




        class VolumesDrawToggleCommand : WireframesDrawToggleCommand
        {
            protected override string WhatFor => "Volumes";
            public VolumesDrawToggleCommand()
            {
                AllowXray = true;
            }
            public override bool WireframesEnabled
            {
                get => Instance.WireframesEnabled;
                set => Instance.WireframesEnabled = value;
            }
            public override Mesh[] BoundingBoxes => Instance.BoundingBoxes;
        }
    }
}
