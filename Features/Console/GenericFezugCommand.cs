
namespace FEZUG.Features.Console
{
    internal class GenericFezugCommand : IFezugCommand
    {
        public string Name { get; }

        public string HelpText { get; }

        private Func<string[], List<string>> AutocompleteProvider;
        public List<string> Autocomplete(string[] args)
        {
            return AutocompleteProvider(args);
        }

        private Func<string[], bool> ExecuteCommand;
        public bool Execute(string[] args)
        {
            return ExecuteCommand(args);
        }

        public GenericFezugCommand(string name, string helpText, Func<string[], List<string>> autocompleteProvider, Func<string[], bool> executeCommand)
        {
            this.Name = name;
            this.HelpText = helpText;
            AutocompleteProvider = autocompleteProvider ?? (_ => []);
            ExecuteCommand = executeCommand ?? (_ => false);
        }
    }
}