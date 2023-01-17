using System;
using FezEngine.Components;
using FezEngine.Services;
using FezEngine.Structure;
using FezEngine.Structure.Input;
using FezEngine.Tools;
using FezGame.Services;
using FEZUG.Features.Console;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using System.Collections.Generic;
using System.Globalization;

namespace FEZUG.Features
{
    internal class Kill : IFezugCommand
    {
        public string Name => "kill";

        public string HelpText => "kill - the most important console command from the source engine";

        [ServiceDependency]
        public IPlayerManager PlayerManager { private get; set; }

        [ServiceDependency]
        public IInputManager InputManager { private get; set; }

        [ServiceDependency]
        public IContentManagerProvider ContentManagerProvider { private get; set; }

        private SoundEffect thudSound;

        public List<string> Autocomplete(string[] args)
        {
            return null;
        }

        public bool Execute(string[] args)
        {
            if (PlayerManager.Grounded)
            {
                if (thudSound == null)
                {
                    // This gives a NullReferenceException if we put this in a constructor so lazily load it here
                    thudSound = ContentManagerProvider.Global.Load<SoundEffect>("Sounds/Gomez/CrashLand");
                }
                thudSound.EmitAt(PlayerManager.Position).NoAttenuation = true;
                InputManager.ActiveGamepad.Vibrate(VibrationMotor.RightHigh, 1.0, TimeSpan.FromSeconds(0.5), EasingType.Quadratic);
                InputManager.ActiveGamepad.Vibrate(VibrationMotor.LeftLow, 1.0, TimeSpan.FromSeconds(0.35));
                PlayerManager.Action = FezGame.Structure.ActionType.Dying;
                PlayerManager.Velocity *= Vector3.UnitY;
            }
            else
            {
                PlayerManager.Action = FezGame.Structure.ActionType.FreeFalling;
            }
            return true;
        }
    }
}
