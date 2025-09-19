using FezEngine.Services;
using FezEngine.Tools;
using FezGame.Services;
using FEZUG.Features.Console;
using System.Globalization;

// Jenna1337 was here
/* TODO
 * useful settings in Settings:
 * (
 * minus denotes restart required,
 * plus denotes FEZUG command has been added
 * question denotes needs testing
 * slash denotes unused
 * )
public ScreenMode ScreenMode
public ScaleMode ScaleMode
+ public int Width
+ public int Height
- public bool HighDPI
+ public float SoundVolume
+ public float MusicVolume
? public bool Vibration
public bool PauseOnLostFocus
- public bool Singlethreaded
public float Brightness
public int DeadZone
public bool DisableController
/ public bool InvertMouse
public bool InvertLook
public bool InvertLookX
public bool InvertLookY
public bool VSync
public bool HardwareInstancing
- public int MultiSampleCount
- public bool MultiSampleOption
public bool Lighting
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
    public abstract class FezugVariableBoolean : IFezugCommand
    {
        readonly Dictionary<string, Func<string>> options;

        public abstract bool Value { get; set; }
        public abstract string Name { get; }
        public abstract string FlavorText { get; }
        public abstract string HelpTextFlavor { get; }
        public string HelpText => Name + " <on/off/toggle> - changes the state of " + HelpTextFlavor;

        public List<string> Autocomplete(string[] args)
        {
            return new string[] { "on", "off", "toggle" }.Where(s => s.StartsWith(args[0])).ToList();
        }

        public FezugVariableBoolean()
        {
            ServiceHelper.InjectServices(this);

            options = new(){
                {"on", ()=>{ Value = true; return " has been enabled."; }},
                {"off", ()=>{ Value = false; return " has been disabled."; }},
                {"toggle", ()=>{ return $" has been toggled to {((Value = !Value) ? "enable" : "disable")}."; }}
            };
        }

        public bool Execute(string[] args)
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
            get => SettingsManager.Settings.Vibration;
            set
            {
                SettingsManager.Settings.Vibration = value;
            }
        }
    }
}
