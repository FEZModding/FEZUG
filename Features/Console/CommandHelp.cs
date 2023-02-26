using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FEZUG.Features.Console
{
    internal class CommandHelp : IFezugCommand
    {
        public string Name => "help";

        public string HelpText => "help [page/command] - displays given page of help or tooltip for given command.";

        public int CommandListPageSize { get; set; }

        public CommandHelp()
        {
            CommandListPageSize = 10;
        }

        public List<string> Autocomplete(string[] args) => null;

        public bool Execute(string[] args)
        {
            var cmdList = FezugConsole.Instance.Handler.Commands;
            var varList = FezugVariable.DefinedList;

            int pageNumber = 1;
            if (args.Length == 0 || int.TryParse(args[0], out pageNumber))
            {
                var helpStrings = new List<string>();
                helpStrings.AddRange(cmdList.Select(cmd => cmd.HelpText));
                helpStrings.AddRange(varList.Select(var => $"{var.Name} - {var.HelpText}"));
                helpStrings = helpStrings.OrderBy(str => str).ToList();

                int pageCount = (int)Math.Ceiling(helpStrings.Count / (float)CommandListPageSize);
                pageNumber = Math.Min(Math.Max(pageNumber, 1), pageCount);

                FezugConsole.Print($"=== Help - page {pageNumber}/{pageCount} ===");

                var pageStart = (pageNumber - 1) * CommandListPageSize;
                var pageEnd = Math.Min(helpStrings.Count, pageNumber * CommandListPageSize);
                for(var i = pageStart; i < pageEnd; i++)
                {
                    FezugConsole.Print(helpStrings[i]);
                }

                return true;
            }
            else 
            {
                var validCommands = cmdList.Where(cmd => cmd.Name.Equals(args[0], StringComparison.OrdinalIgnoreCase));
                var validVariables = varList.Where(var => var.Name.Equals(args[0], StringComparison.OrdinalIgnoreCase));

                if (validCommands.Count() == 0 && validVariables.Count() == 0)
                {
                    FezugConsole.Print($"Command \"{args[0]}\" hasn't been found.", FezugConsole.OutputType.Warning);
                    return false;
                }
                else if(validCommands.Count() > 0)
                {
                    FezugConsole.Print(validCommands.First().HelpText);
                    return true;
                }
                else if(validVariables.Count() > 0)
                {
                    FezugConsole.Print($"{validVariables.First().Name} - {validVariables.First().HelpText}");
                    return true;
                }

                return false;
            }

        }
    }
}
