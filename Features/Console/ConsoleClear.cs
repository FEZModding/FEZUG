using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FEZUG.Features.Console
{
    internal class ConsoleClear : IConsoleCommand
    {
        public string Name => "clear";
        public string HelpText => "clear - clears console output";

        public List<string> Autocomplete(string args) => null;

        public bool Execute(string[] args)
        {
            FEZUG.Console.ClearOutput();
            return true;
        }
    }
}
