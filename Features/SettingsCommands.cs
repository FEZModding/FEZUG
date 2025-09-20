using FezEngine.Services;
using FezEngine.Tools;
using FEZUG.Features.Console;
using Microsoft.Xna.Framework.Graphics;
using System.Globalization;

// Jenna1337 was here
/* TODO
 * useful settings in Settings:
 * (
 * minus denotes restart required,
 * plus denotes FEZUG command has been added,
 * question denotes needs testing,
 * slash denotes unused,
 * exclamation denotes unsafe
 * )
+ public ScreenMode ScreenMode
+ public ScaleMode ScaleMode
+ public int Width
+ public int Height
- public bool HighDPI
+ public float SoundVolume
+ public float MusicVolume
+ public bool Vibration
+ public bool PauseOnLostFocus
- public bool Singlethreaded
+ public float Brightness
+ public int DeadZone
- public bool DisableController
/ public bool InvertMouse
+ public bool InvertLook
+ public bool InvertLookX
+ public bool InvertLookY
+ public bool VSync
! public bool HardwareInstancing
- public int MultiSampleCount
- public bool MultiSampleOption
+ public bool Lighting
- public bool DisableSteamworks
 */
namespace FEZUG.Features
{
    public abstract class FezugVariableNumber : IFezugCommand
    {
        public abstract float Value { get; set; }
        public abstract string Name { get; }
        public abstract string FlavorText { get; }
        public abstract string HelpTextFlavor { get; }
        public string HelpText => Name + " <value> - Sets " + HelpTextFlavor;
        public int DecimalPlaces
        {
            get => decimalPlaces;
            set
            {
                if (decimalPlaces >= 0)
                {
                    decimalPlaces = value;
                    if (decimalPlaces != 0)
                    {
                        numFormatString = "0." + new string('0', decimalPlaces);
                    }else{
                        numFormatString = "0";
                    }
                }
            }
        }
        private string numFormatString = "0.000";
        private int decimalPlaces = 3;

        public FezugVariableNumber()
        {
            ServiceHelper.InjectServices(this);
        }

        public List<string> Autocomplete(string[] args)
        {
            
            string valStr = Value.ToString(numFormatString, CultureInfo.InvariantCulture);
            if (valStr.StartsWith(args[0])) return new List<string> { valStr };
            return null;
        }

        public bool Execute(string[] args)
        {
            if (args.Length != 1)
            {
                FezugConsole.Print($"Incorrect number of parameters: '{args.Length}'", FezugConsole.OutputType.Warning);
                return false;
            }

            if (!float.TryParse(args[0], NumberStyles.Number, CultureInfo.InvariantCulture, out float timescale))
            {
                FezugConsole.Print($"Incorrect {FlavorText.ToLowerInvariant()}: '{args[0]}'", FezugConsole.OutputType.Warning);
                return false;
            }

            Value = timescale;

            FezugConsole.Print($"{FlavorText} has been set to {Value.ToString(numFormatString, CultureInfo.InvariantCulture)}");

            return true;
        }

    }
    public abstract class FezugVariableMultipleChoice<T> : IFezugCommand
    {
        protected Dictionary<string, Func<string>> options;

        public abstract T Value { get; set; }
        public abstract string Name { get; }
        public abstract string FlavorText { get; }
        public abstract string HelpTextFlavor { get; }
        public string HelpText => Name + " <" + string.Join("/", options.Keys) + "> - changes the state of " + HelpTextFlavor;

        public List<string> Autocomplete(string[] args)
        {
            return options.Keys.Where(s => s.StartsWith(args[0], true, CultureInfo.InvariantCulture)).ToList();
        }

        public FezugVariableMultipleChoice()
        {
            ServiceHelper.InjectServices(this);
        }

        public virtual bool Execute(string[] args)
        {
            if (args.Length != 1)
            {
                FezugConsole.Print($"Incorrect number of parameters: '{args.Length}'", FezugConsole.OutputType.Warning);
                return false;
            }

            if (options.TryGetValue(args[0], out var action))
            {
                FezugConsole.Print(FlavorText + action());
            }
            else
            {
                FezugConsole.Print($"Unknown parameter: '{args[0]}'", FezugConsole.OutputType.Warning);
                return false;
            }

            return true;
        }
    }

    public abstract class FezugVariableBoolean : FezugVariableMultipleChoice<bool>
    {
        public FezugVariableBoolean() : base()
        {
            options = new(){
                {"on", ()=>{ Value = true; return " has been enabled."; }},
                {"off", ()=>{ Value = false; return " has been disabled."; }},
                {"toggle", ()=>{ return $" has been toggled to {((Value = !Value) ? "enable" : "disable")}."; }}
            };
        }
    }
    public abstract class FezugVariableEnum<T> : FezugVariableMultipleChoice<T> where T : struct
    {
        public FezugVariableEnum() : base()
        {
            Type type = typeof(T);
            if (type.IsEnum == true)
            {
                options = Enum.GetNames(type).ToDictionary<string, string, Func<string>>(name => name,
                    name => {
                        var value = Enum.Parse(type, name);
                        return () => {
                            Value = (T)value;
                            return " has been set to " + name;
                        };
                    });
            }
            else
            {
                throw new ArgumentException("Type \"" + type.Name + "\" is not an enum");
            }
        }
        public override bool Execute(string[] args)
        {
            if (args.Length != 1)
            {
                FezugConsole.Print($"Incorrect number of parameters: '{args.Length}'", FezugConsole.OutputType.Warning);
                return false;
            }

            if (Enum.TryParse<T>(args[0], true, out T value) && Enum.IsDefined(typeof(T), value))
            {
                Value = (T)value;
                FezugConsole.Print(FlavorText + " has been set to " + Enum.GetName(typeof(T), value));
            }
            else
            {
                FezugConsole.Print($"Unknown parameter: '{args[0]}'", FezugConsole.OutputType.Warning);
                return false;
            }

            return true;
        }
    }
    //TODO clean up
    //public class StereoscopyToggleTest : FezugVariableBoolean
    //{
    //    public override string Name => "v3d";
    //    public override string FlavorText => "Stereoscopy mode";
    //    public override string HelpTextFlavor => "stereoscopy mode";


    //    [ServiceDependency]
    //    public IGameStateManager GameState { private get; set; }

    //    public override bool Value { get => GameState.StereoMode; set => GameState.StereoMode = value; }
    //}
    public class SettingScreenMode : FezugVariableEnum<ScreenMode>
    {
        public override string Name => "setting_screen_mode";
        public override string FlavorText => "Screen mode";
        public override string HelpTextFlavor => "the screen mode. like full screen vs windowed, etc.";

        public override ScreenMode Value {
            get => SettingsManager.Settings.ScreenMode;
            set
            {
                SettingsManager.Settings.ScreenMode = value;
                SettingsManager.Apply();
            }
        }
    }
    public class SettingScaleMode : FezugVariableEnum<ScaleMode>
    {
        public override string Name => "setting_scale_mode";
        public override string FlavorText => "Scale mode";
        public override string HelpTextFlavor => "the scaling mode. like pixel-perfect scaling";

        public override ScaleMode Value {
            get => SettingsManager.Settings.ScaleMode;
            set
            {
                SettingsManager.Settings.ScaleMode = value;
                SettingsManager.Apply();
            }
        }
    }
    public class SettingWindowWidth : FezugVariableNumber
    {
        public override string Name => "setting_width";
        public override string FlavorText => "Window width";
        public override string HelpTextFlavor => "the width of the window";
        public SettingWindowWidth() : base()
        {
            DecimalPlaces = 0;
        }

        public override float Value
        {
            get => SettingsManager.Settings.Width;
            set
            {
                SettingsManager.Settings.Width = (int)value;
                SettingsManager.Apply();
            }
        }
    }
    public class SettingWindowHeight : FezugVariableNumber
    {
        public override string Name => "setting_height";
        public override string FlavorText => "Window height";
        public override string HelpTextFlavor => "the height of the window";
        public SettingWindowHeight() : base()
        {
            DecimalPlaces = 0;
        }

        public override float Value
        {
            get => SettingsManager.Settings.Height;
            set
            {
                SettingsManager.Settings.Height = (int)value;
                SettingsManager.Apply();
            }
        }
    }
    public class SettingVolumeSound : FezugVariableNumber
    {
        public override string Name => "setting_volume_sound";
        public override string FlavorText => "Sound volume";
        public override string HelpTextFlavor => "the volume of game sounds";

        [ServiceDependency]
        public ISoundManager SoundManager { private get; set; }

        public override float Value
        {
            get => SettingsManager.Settings.SoundVolume;
            set
            {
                SettingsManager.Settings.SoundVolume = value;
                SoundManager.SoundEffectVolume = value;
            }
        }
    }
    public class SettingVolumeMusic : FezugVariableNumber
    {
        public override string Name => "setting_volume_music";
        public override string FlavorText => "Music volume";
        public override string HelpTextFlavor => "the volume of game music";

        [ServiceDependency]
        public ISoundManager SoundManager { private get; set; }

        public override float Value
        {
            get => SettingsManager.Settings.MusicVolume;
            set
            {
                SettingsManager.Settings.MusicVolume = value;
                SoundManager.MusicVolume = value;
            }
        }
    }
    public class SettingVibration : FezugVariableBoolean
    {
        public override string Name => "setting_vibration";
        public override string FlavorText => "Controller vibration";
        public override string HelpTextFlavor => "the controller vibration setting";

        public override bool Value
        {
            get => SettingsManager.Settings.Vibration; set => SettingsManager.Settings.Vibration = value;
        }
    }
    public class SettingPauseOnLostFocus : FezugVariableBoolean
    {
        public override string Name => "setting_pauseonlostfocus";
        public override string FlavorText => "Pause on lost focus";
        public override string HelpTextFlavor => "if the game pauses when the window loses focus";

        public override bool Value
        {
            get => SettingsManager.Settings.PauseOnLostFocus; set => SettingsManager.Settings.PauseOnLostFocus = value;
        }
    }
    public class SettingInvertLook : FezugVariableBoolean
    {
        public override string Name => "setting_invertlook";
        public override string FlavorText => "First-person look inverted?";
        public override string HelpTextFlavor => "invert X and Y axes when looking around in first-person mode setting";

        private static bool LastValue = false;

        public override bool Value
        {
            get => LastValue; set => LastValue = SettingsManager.Settings.InvertLook = value;
        }
    }
    public class SettingInvertLookX : FezugVariableBoolean
    {
        public override string Name => "setting_invertlookx";
        public override string FlavorText => "First-person look X axis inverted?";
        public override string HelpTextFlavor => "invert X axis when looking around in first-person mode setting";

        public override bool Value
        {
            get => SettingsManager.Settings.InvertLookX; set => SettingsManager.Settings.InvertLookX = value;
        }
    }
    public class SettingInvertLookY : FezugVariableBoolean
    {
        public override string Name => "setting_invertlooky";
        public override string FlavorText => "First-person look Y axis inverted?";
        public override string HelpTextFlavor => "invert Y axis when looking around in first-person mode setting";

        public override bool Value
        {
            get => SettingsManager.Settings.InvertLookY; set => SettingsManager.Settings.InvertLookY = value;
        }
    }
    public class SettingVSync : FezugVariableBoolean
    {
        public override string Name => "setting_vsync";
        public override string FlavorText => "VSync";
        public override string HelpTextFlavor => "VSync setting";

        public override bool Value
        {
            get => SettingsManager.Settings.VSync;
            set
            {
                SettingsManager.Settings.VSync = value;
                SettingsManager.Apply();
            }
        }
    }
    public class SettingLighting : FezugVariableBoolean
    {
        public override string Name => "setting_lighting";
        public override string FlavorText => "Lighting setting";
        public override string HelpTextFlavor => "lighting setting";

        public override bool Value
        {
            get => SettingsManager.Settings.Lighting; set => SettingsManager.Settings.Lighting = value;
        }
    }
    public class SettingBrightness : FezugVariableNumber
    {
        public override string Name => "setting_brightness";
        public override string FlavorText => "Brightness setting";
        public override string HelpTextFlavor => "brightness setting";

        public override float Value
        {
            get => SettingsManager.Settings.Brightness;
            set
            {
                SettingsManager.Settings.Brightness = value;
                ServiceHelper.Get<GraphicsDevice>().SetGamma(value);
            }
        }
    }
    public class SettingDeadZone : FezugVariableNumber
    {
        public override string Name => "setting_deadzone";
        public override string FlavorText => "Controller deadzone percentage";
        public override string HelpTextFlavor => "controller deadzone percentage";

        public SettingDeadZone() : base()
        {
            DecimalPlaces = 0;
        }

        public override float Value
        {
            get => SettingsManager.Settings.DeadZone;
            set
            {
                SettingsManager.Settings.DeadZone = (int)value;
            }
        }
    }
}
