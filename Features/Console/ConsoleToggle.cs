using FezEngine.Components;

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
