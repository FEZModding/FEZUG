﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FEZUG.Features.Console
{
    internal class ConsoleClear : IFezugCommand
    {
        public string Name => "clear";
        public string HelpText => "clear - clears console output";

        public List<string> Autocomplete(string[] args) => null;

        public bool Execute(string[] args)
        {
            FezugConsole.Clear();
            return true;
        }
    }
}
