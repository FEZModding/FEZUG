using FezEngine.Services;
using FezEngine.Tools;
using FEZUG.Features.Console;
using System.Globalization;

namespace FEZUG.Features
{
    internal class TimeSet : IFezugCommand
    {
        public string Name => "time";

        public string HelpText => "time <set/speed> <value/real> - sets game's day time/speed";

        [ServiceDependency]
        public ITimeManager TimeManager { private get; set; }

        public List<string> Autocomplete(string[] args)
        {
            if(args.Length == 1)
            {
                return [.. new string[] { "set", "speed" }.Where(s => s.StartsWith(args[0]))];
            }else if(args.Length == 2)
            {
                if (args[0].Equals("set"))
                {
                    return [.. new string[]
                    {
                        TimeManager.CurrentTime.ToString("HH:mm"),
                        "dawn", "day", "dusk", "night", "real"
                    }.Where(s => s.StartsWith(args[1]))];
                }
                if (args[0].Equals("speed") && args[1].Length == 0)
                {
                    float curTime = TimeManager.TimeFactor / TimeManager.DefaultTimeFactor;
                    return [curTime.ToString("0.000", CultureInfo.InvariantCulture) , "real"];
                }
            }
            return null;
        }

        public bool Execute(string[] args)
        {
            if (args.Length != 2)
            {
                FezugConsole.Print($"Incorrect number of parameters: '{args.Length}'", FezugConsole.OutputType.Warning);
                return false;
            }

            if(args[0] == "set")
            {
                DateTime dateTime = DateTime.Now;
                if(!DateTime.TryParseExact(args[1], "H:mm", null, DateTimeStyles.None, out dateTime)){
                    switch (args[1])
                    {
                        case "real": dateTime = DateTime.Now; break;
                        case "dawn": dateTime = DateTime.ParseExact("6:00", "H:mm", null); break;
                        case "day": dateTime = DateTime.ParseExact("12:00", "H:mm", null); break;
                        case "dusk": dateTime = DateTime.ParseExact("18:00", "H:mm", null); break;
                        case "night": dateTime = DateTime.ParseExact("0:00", "H:mm", null); break;
                        default:
                            FezugConsole.Print($"Invalid time has been given.", FezugConsole.OutputType.Warning);
                            return false;
                    }
                }
                TimeManager.CurrentTime = dateTime;
                FezugConsole.Print($"Time has been set to {dateTime:H:mm}.");
            }
            else if(args[0] == "speed")
            {
                if(float.TryParse(args[1], out float speed))
                {
                    TimeManager.TimeFactor = TimeManager.DefaultTimeFactor * speed;
                }
                else if(args[1] == "real")
                {
                    TimeManager.TimeFactor = 1.0f;
                }
                else
                {
                    FezugConsole.Print($"Incorrect speed value.", FezugConsole.OutputType.Warning);
                }
                string speedString = (TimeManager.TimeFactor / TimeManager.DefaultTimeFactor).ToString("0.0####", CultureInfo.InvariantCulture);
                FezugConsole.Print($"Day time speed has been set to {speedString}.");
            }
            else
            {
                return false;
            }


            return true;
        }
    }
}
