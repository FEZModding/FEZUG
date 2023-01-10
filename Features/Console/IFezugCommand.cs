using System.Collections.Generic;

namespace FEZUG.Features.Console
{
    public interface IFezugCommand
    {
        string Name { get; }
        string HelpText { get; }
        bool Execute(string[] args);
        List<string> Autocomplete(string[] args);
    }
}
