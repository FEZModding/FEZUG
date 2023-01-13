using FezEngine.Services;
using FezEngine.Tools;
using FEZUG.Features.Console;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace FEZUG.Features
{
    internal class Timescaler : IFezugCommand
    {
        public static float Timescale = 1.0f;

        public string Name => "timescale";

        public string HelpText => "timescale <value> - changes game's simulation scale";

        [ServiceDependency]
        public ISoundManager SoundManager { private get; set; }

        public List<string> Autocomplete(string[] args)
        {
            string timescaleStr = Timescale.ToString("0.000", CultureInfo.InvariantCulture);
            if (timescaleStr.StartsWith(args[0])) return new List<string> { timescaleStr };
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
                FezugConsole.Print($"Incorrect timescale: '{args[0]}'", FezugConsole.OutputType.Warning);
                return false;
            }

            Timescale = timescale;

            FezugConsole.Print($"Timescale has been set to {Timescale.ToString("0.000", CultureInfo.InvariantCulture)}");

            return true;
        }
    }
}
