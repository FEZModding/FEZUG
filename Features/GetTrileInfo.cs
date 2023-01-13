using FezEngine.Structure;
using FezEngine.Tools;
using FezGame.Services;
using FEZUG.Features.Console;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FEZUG.Features
{
    internal class GetTrileInfo : IFezugCommand
    {
        public string Name => "gettrile";

        public string HelpText => "gettrile [x] [y] [z] - get trile info at given coordinates";

        [ServiceDependency]
        public IGameLevelManager LevelManager { private get; set; }

        public List<string> Autocomplete(string[] args) => null;

        public bool Execute(string[] args)
        {
            if (args.Length != 3)
            {
                FezugConsole.Print($"Incorrect number of parameters: '{args.Length}'", FezugConsole.OutputType.Warning);
                return false;
            }

            if (!int.TryParse(args[0], NumberStyles.Number, CultureInfo.InvariantCulture, out int x))
            {
                FezugConsole.Print($"Incorrect coordinate: '{args[0]}'", FezugConsole.OutputType.Warning);
                return false;
            }
            if (!int.TryParse(args[1], NumberStyles.Number, CultureInfo.InvariantCulture, out int y))
            {
                FezugConsole.Print($"Incorrect coordinate: '{args[1]}'", FezugConsole.OutputType.Warning);
                return false;
            }
            if (!int.TryParse(args[2], NumberStyles.Number, CultureInfo.InvariantCulture, out int z))
            {
                FezugConsole.Print($"Incorrect coordinate: '{args[2]}'", FezugConsole.OutputType.Warning);
                return false;
            }

            var pos = new TrileEmplacement(x, y, z);
            if (!LevelManager.Triles.ContainsKey(pos))
            {
                FezugConsole.Print($"No trile at coordinates [{x}, {y}, {z}]", FezugConsole.OutputType.Warning);
                return false;
            }
            var trileInstance = LevelManager.Triles[pos];
            var trile = trileInstance.Trile;
            FezugConsole.Print($"Instance ID: {trileInstance.TrileId}");
            FezugConsole.Print($"Name: {trile.Name}");
            FezugConsole.Print($"ActorType: {trile.ActorSettings.Type}");
            FezugConsole.Print($"Immaterial: {trile.Immaterial}");
            FezugConsole.Print($"SeeThrough: {trile.SeeThrough}");

            return true;
        }
    }
}
