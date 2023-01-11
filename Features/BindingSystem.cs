using Common;
using FezEngine.Components;
using FezEngine.Tools;
using FEZUG.Features.Console;
using FEZUG.Helpers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FEZUG.Features
{
    internal class BindingSystem : IFezugFeature
    {
        public Dictionary<Keys, string> Binds { get; private set; }

        public static BindingSystem Instance;

        [ServiceDependency]
        public IInputManager InputManager { get; set; }

        public BindingSystem()
        {
            Instance = this;
            Binds = new Dictionary<Keys, string>();
        }

        public void Initialize() { }

        public void Update(GameTime gameTime)
        {
            if (!((InputManager)InputManager).Enabled) return;

            foreach (var bindPair in Binds)
            {
                if (InputHelper.IsKeyPressed(bindPair.Key))
                {
                    FezugConsole.ExecuteCommand(bindPair.Value);
                }
            }
        }

        public void Draw(GameTime gameTime) { }

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
            if (command.Length == 0) Instance.Binds.Remove(key);
            else Instance.Binds.Add(key, command);
        }

        internal class BindCommand : IFezugCommand
        {
            public string Name => "bind";
            public string HelpText => "bind <key> <command> - binds a command to specified keyboard key";

            public List<string> Autocomplete(string[] args)
            {
                if (args.Length == 1)
                {
                    return Enum.GetNames(typeof(Keys)).Select(s=>s.ToLower()).Where(s => s.StartsWith(args[0], StringComparison.OrdinalIgnoreCase)).ToList();
                }

                if (args.Length == 2 && Enum.TryParse<Keys>(args[0], true, out var key)
                && HasBind(key) && GetBind(key).StartsWith(args[1], StringComparison.OrdinalIgnoreCase))
                {
                    return new List<string> { GetBind(key) };
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

                if (!Enum.TryParse<Keys>(args[0], true, out var key))
                {
                    FezugConsole.Print($"Invalid key: {args[0]}.", FezugConsole.OutputType.Warning);
                    return false;
                }

                if (args.Length == 1)
                {
                    if (HasBind(key))
                    {
                        FezugConsole.Print($"Key {key} is bound to command \"{ GetBind(key)}\".");
                    }
                    else
                    {
                        FezugConsole.Print($"No command has been bound to key {key}.");
                    }
                }

                if (args.Length == 2)
                {
                    SetBind(key, args[1]);
                    FezugConsole.Print($"Command has been bound to key {key}.");
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
                    return Enum.GetNames(typeof(Keys)).Select(s => s.ToLower()).Where(s => s.StartsWith(args[0], StringComparison.OrdinalIgnoreCase)).ToList();
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

                if (!Enum.TryParse<Keys>(args[0], true, out var key))
                {
                    FezugConsole.Print($"Invalid key: {args[0]}.", FezugConsole.OutputType.Warning);
                    return false;
                }

                if (HasBind(key))
                {
                    SetBind(key, "");
                    FezugConsole.Print($"Key {key} has been unbound.");
                }
                else
                {
                    FezugConsole.Print($"No command has been bound to key {key}.");
                }

                return true;
            }
        }
    }
}
