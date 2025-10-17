using FezEngine.Services;
using FezEngine.Tools;
using FezGame.Services;
using FEZUG.Features.Console;
using Microsoft.Xna.Framework;
using System.Globalization;

namespace FEZUG.Features
{
    internal class Teleport : IFezugCommand
    {
        public string Name => "tp";

        public string HelpText => "tp <x> <y> <z> - teleports Gomez to given coordinates";

        [ServiceDependency]
        public IPlayerManager PlayerManager { private get; set; }

        [ServiceDependency]
        public IDefaultCameraManager CameraManager { private get; set; }

        public List<string> Autocomplete(string[] args)
        {
            if (args.Length == 0 || args[args.Length - 1].Length > 0) return null;

            var pos = PlayerManager.Position;
            float value;
            switch (args.Length)
            {
                case 1: value = pos.X; break;
                case 2: value = pos.Y; break;
                case 3: value = pos.Z; break;
                default: return null;
            }

            return [value.ToString("0.000", CultureInfo.InvariantCulture)];
        }

        public bool Execute(string[] args)
        {
            if (!TryParseCoords(args, PlayerManager.Position, out Vector3 coords))
            {
                return false;
            }

            PlayerManager.Position = coords;
            FezugConsole.Print($"Player teleported to coordinates: (X:{coords.X}, Y:{coords.Y}, Z:{coords.Z})");
            return true;
        }

        internal const string BASEVAL_CHAR = "~";
        public static bool TryParseCoord(string arg, in float baseVal, out float c)
        {
            if (arg == BASEVAL_CHAR)
            {
                c = baseVal;
                return true;
            }
            c = 0;
            if (arg.StartsWith(BASEVAL_CHAR))
            {
                c = baseVal;
                arg = arg.Substring(BASEVAL_CHAR.Length);
            }
            if (!float.TryParse(arg, NumberStyles.Number, CultureInfo.InvariantCulture, out float v))
            {
                FezugConsole.Print($"Incorrect coordinate: '{arg}'", FezugConsole.OutputType.Warning);
                return false;
            }
            c += v;
            return true;
        }
        public static bool TryParseCoords(string[] args, Vector3 baseCoords, out Vector3 coords)
        {
            coords = default;
            if (args.Length != 3)
            {
                FezugConsole.Print($"Incorrect number of parameters: '{args.Length}'", FezugConsole.OutputType.Warning);
                return false;
            }

            if (TryParseCoord(args[0], in baseCoords.X, out float x)
                && TryParseCoord(args[1], in baseCoords.Y, out float y)
                && TryParseCoord(args[2], in baseCoords.Z, out float z)
                )
            {
                coords = new Vector3(x, y, z);
                return true;
            }
            return false;
        }
    }
}
