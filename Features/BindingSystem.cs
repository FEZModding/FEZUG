using Common;
using FezEngine.Components;
using FezEngine.Tools;
using FEZUG.Features.Console;
using FEZUG.Helpers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace FEZUG.Features
{
    using BindList = Dictionary<Keys, string>;

    internal class BindingSystem : IFezugFeature
    {
        public const string BindConfigFileName = "FezugBinds";
        private const string GamePadPrefix = "Gamepad";
        private const int GamePadKeysOffset = 0x300000;

        private InputHelper InputHelper { get; } = InputHelper.Instance;
        
        public BindList Binds { get; private set; }

        public static BindingSystem Instance;

        [ServiceDependency]
        public IInputManager InputManager { get; set; }

        public BindingSystem()
        {
            Instance = this;
            Binds = [];
        }

        public void Initialize() {
            LoadBinds();
        }

        public void Update(GameTime gameTime)
        {
            foreach (var bindPair in Binds)
            {
                if (InputHelper.IsKeyPressed(bindPair.Key) || TryConvertKeysToGamepadButtons(bindPair.Key, out var b ) && InputHelper.IsButtonPressed(b))
                {
                    FezugConsole.ExecuteCommand(bindPair.Value);
                }
            }
        }

        public void DrawHUD(GameTime gameTime) { }

        public void DrawLevel(GameTime gameTime) { }

        public static bool HasBind(Keys key)
        {
            return Instance.Binds.ContainsKey(key);
        }

        public static string GetBind(Keys key)
        {
            if (HasBind(key)) return Instance.Binds[key];
            else return "";
        }

        public static void SetBind(Keys key, string command)
        {
            if (command.Length == 0 || Instance.Binds.ContainsKey(key))
                Instance.Binds.Remove(key);
            if (command.Length > 0)
                Instance.Binds.Add(key, command);


            string configPath = Path.Combine(Util.LocalConfigFolder, BindConfigFileName);
            SaveBinds();
        }

        private static string GetBindsFilePath()
        {
            return Path.Combine(Util.LocalConfigFolder, BindConfigFileName);
        }

        private static void SaveBinds()
        {
            using StreamWriter bindFile = new(GetBindsFilePath());
            foreach (var bind in Instance.Binds)
            {
                bindFile.WriteLine($"{GetButtonText(bind.Key)} {bind.Value}");
            }
        }

        private static void LoadBinds()
        {
            var bindFilePath = GetBindsFilePath();
            if (!File.Exists(bindFilePath)) return;
            var bindFileLines = File.ReadAllLines(bindFilePath);
            foreach(var line in bindFileLines)
            {
                string[] tokens = line.Split([' '], 2);
                if (tokens.Length < 2) continue;

                if(TryParseBindName(tokens[0], out Keys key))
                {
                    Instance.Binds.Add(key, tokens[1]);
                }
            }
        }

        internal static string GetButtonText(Keys key)
        {
            return TryConvertKeysToGamepadButtons(key, out var b) ? GamePadPrefix + b.ToString() : key.ToString();
        }
        internal static string GetButtonText(Buttons button)
        {
            return GamePadPrefix + button.ToString();
        }
        private static bool TryConvertKeysToGamepadButtons(Keys key, out Buttons button)
        {
            if ((int)key >= GamePadKeysOffset)
            {
                button = (Buttons)Math.Pow(2, (int)key - GamePadKeysOffset);
                return true;
            }
            button = 0;
            return false;
        }
        private static List<string> GetAutoCompleteOptions(string arg)
        {
            return [..Enum.GetNames(typeof(Keys)).Select(s => s.ToLower()).Concat(Enum.GetNames(typeof(Buttons)).Select(b => GamePadPrefix + b)).Where(s => s.StartsWith(arg, StringComparison.OrdinalIgnoreCase))] ;
        }
        private static bool TryParseBindName(string str, out Keys key)
        {
            bool isInt;
            if (isInt = int.TryParse(str, out int i) && 0 <= i && i <= 9)
            {
                key = Keys.D0 + i;
                return true;
            }
            else if (!isInt && str.StartsWith(GamePadPrefix, StringComparison.InvariantCultureIgnoreCase) && Enum.TryParse<Buttons>(str.Substring(GamePadPrefix.Length), true, out var b))
            {
                //Buttons is a bitmask
                key = (Keys)(GamePadKeysOffset + Math.Log((int)b, 2));
                return true;
            }
            else if (Enum.TryParse<Keys>(str, true, out key))
            {
                return true;
            }
            return false;
        }

        internal class BindCommand : IFezugCommand
        {
            public string Name => "bind";
            public string HelpText => "bind <key> <command> - binds a command to specified keyboard key";

            public List<string> Autocomplete(string[] args)
            {
                if (args.Length == 1)
                {
                    return GetAutoCompleteOptions(args[0]);
                }

                if (args.Length == 2 && Enum.TryParse<Keys>(args[0], true, out var key)
                && HasBind(key) && GetBind(key).StartsWith(args[1], StringComparison.OrdinalIgnoreCase))
                {
                    return [GetBind(key)];
                }
                return null;
            }

            public bool Execute(string[] args)
            {
                if (args.Length < 1 || args.Length > 2)
                {
                    FezugConsole.Print($"Incorrect number of parameters: '{args.Length}'", FezugConsole.OutputType.Warning);
                    return false;
                }

                if (!TryParseBindName(args[0], out var key))
                {
                    FezugConsole.Print($"Invalid key: {args[0]}.", FezugConsole.OutputType.Warning);
                    return false;
                }

                string keyText = GetButtonText(key);
                if (args.Length == 1)
                {
                    if (HasBind(key))
                    {
                        FezugConsole.Print($"Key {keyText} is bound to command \"{GetBind(key)}\".");
                    }
                    else
                    {
                        FezugConsole.Print($"No command has been bound to key {keyText}.");
                    }
                }

                if (args.Length == 2)
                {
                    SetBind(key, args[1]);
                    if (args[1].Length == 0)
                        FezugConsole.Print($"Key {keyText} has been unbound.");
                    else
                        FezugConsole.Print($"Command has been bound to key {keyText}.");
                }

                return true;
            }
        }

        internal class UnbindCommand : IFezugCommand
        {
            public string Name => "unbind";
            public string HelpText => "unbind <key> - unbinds specified keyboard key";

            public List<string> Autocomplete(string[] args)
            {
                if (args.Length == 1)
                {
                    return GetAutoCompleteOptions(args[0]);
                }

                return null;
            }

            public bool Execute(string[] args)
            {
                if (args.Length != 1)
                {
                    FezugConsole.Print($"Incorrect number of parameters: '{args.Length}'", FezugConsole.OutputType.Warning);
                    return false;
                }

                if (!TryParseBindName(args[0], out var key))
                {
                    FezugConsole.Print($"Invalid key: {args[0]}.", FezugConsole.OutputType.Warning);
                    return false;
                }

                if (HasBind(key))
                {
                    SetBind(key, "");
                    FezugConsole.Print($"Key {GetButtonText(key)} has been unbound.");
                }
                else
                {
                    FezugConsole.Print($"No command has been bound to key {GetButtonText(key)}.");
                }

                return true;
            }
        }
    }
}
