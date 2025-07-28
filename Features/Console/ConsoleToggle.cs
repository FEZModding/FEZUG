using FezEngine.Components;
using FezEngine.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FEZUG.Features.Console
{
    internal class ConsoleToggle : IFezugCommand
    {
        public string Name => "toggleconsole";
        public string HelpText => "toggleconsole - toggles displaying the console";

        public List<string> Autocomplete(string[] args) => null;

        public bool Execute(string[] args)
        {
            FezugConsole.CommandHandler handler = FezugConsole.Instance.Handler;
            handler.Enabled = !handler.Enabled;

            InputManager im = (InputManager)handler.InputManager;
            im.Enabled = !handler.Enabled;

            return true;
        }
    }
}
