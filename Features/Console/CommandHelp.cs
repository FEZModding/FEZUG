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

            int pageNumber = 0;
            if (args.Length == 0 || int.TryParse(args[0], out pageNumber))
            {
                int pageCount = (int)Math.Ceiling(cmdList.Count / (float)CommandListPageSize);
                pageNumber = Math.Min(Math.Max(pageNumber, 0), pageCount-1);

                FezugConsole.Print($"=== Help - page {pageNumber + 1}/{pageCount} ===");

                for(var i = pageNumber * CommandListPageSize; i < cmdList.Count; i++)
                {
                    FezugConsole.Print(cmdList[i].HelpText);
                }

                return true;
            }
            else 
            {
                var validCommands = cmdList.Where(cmd => cmd.Name.Equals(args[0], StringComparison.OrdinalIgnoreCase));

                if(validCommands.Count() == 0)
                {
                    FezugConsole.Print($"Command \"{args[0]}\" hasn't been found.", FezugConsole.OutputType.Warning);
                    return false;
                }
                else
                {
                    FezugConsole.Print(validCommands.First().HelpText);
                    return true;
                }
            }

        }
    }
}
