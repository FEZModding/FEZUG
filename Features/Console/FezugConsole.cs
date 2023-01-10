using Common;
using FezEngine.Components;
using FezEngine.Services;
using FezEngine.Structure.Input;
using FezEngine.Tools;
using FezGame.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace FEZUG.Features.Console
{
    public class FezugConsole : IFezugFeature
    {
        public class ParsedCommand : List<string>
        {
            public string Command => this.Count() == 0 ? "" : this[0];
            public string[] Arguments => this.Count() <= 1 ? new string[0] : this.Skip(1).ToArray();
        }

        public class ParsedCommandSequence : List<ParsedCommand>
        {
            public string Original { get; private set; }

            public ParsedCommandSequence(string raw)
            {
                Original = raw;
                Parse(raw.ToLower());
            }

            private void Parse(string commandString)
            {
                var currentTokens = new ParsedCommand();

                while (commandString.Length > 0)
                {
                    // get nearest split position
                    int split = commandString.IndexOf(' ');
                    if (split < 0) split = commandString.Length;

                    int cmdEndPos = commandString.IndexOf(';');
                    if (cmdEndPos >= 0 && cmdEndPos < split) split = cmdEndPos;

                    int quotePos = commandString.IndexOf('"');
                    int secondQuotePos = commandString.IndexOf('"', quotePos + 1);
                    if (quotePos >= 0 && quotePos < split && secondQuotePos >= 0)
                    {
                        split = quotePos;
                    }

                    char splitChar = split < commandString.Length ? commandString[split] : ';';

                    // handle splitting and separating words
                    bool quote = splitChar == '"';
                    bool endOfCommand = splitChar == ';' || split >= commandString.Length - 1;

                    string splitWord = commandString.Substring(0, split);

                    if (splitWord.Length > 0 || endOfCommand)
                    {
                        currentTokens.Add(splitWord);
                    }

                    if (endOfCommand)
                    {
                        this.Add(currentTokens);
                        currentTokens = new ParsedCommand();
                    }

                    if (quote)
                    {
                        secondQuotePos = commandString.IndexOf('"');
                        string quoteWord = commandString.Substring(0, secondQuotePos);
                        currentTokens.Add(quoteWord);

                        split = secondQuotePos;
                    }

                    if (split + 1 >= commandString.Length)
                    {
                        commandString = "";
                    }
                    else
                    {
                        commandString = commandString.Substring(split + 1);
                    }
                }
            }
        }

        public class AutocompletionManager
        {
            public List<string> SuggestedWords { get; private set; }
            
            public CommandHandler ConsoleLine { get; private set; }

            private ParsedCommandSequence previousCommandSequence;
            private int currentSuggestedWordIndex = 0;

            public string CurrentSuggestedWord => SuggestedWords[currentSuggestedWordIndex];

            public AutocompletionManager(CommandHandler consoleLine)
            {
                ConsoleLine = consoleLine;

                SuggestedWords = new List<string>() { "" };
                previousCommandSequence = new ParsedCommandSequence("");
            }

            public void Refresh(ParsedCommandSequence commandSequence)
            {
                string previousSuggestedWord = CurrentSuggestedWord;

                SuggestedWords.Clear();
                currentSuggestedWordIndex = 0;

                // add an empty word if command sequence ends with whitespace
                if(commandSequence.Count() > 0 && commandSequence.Original.EndsWith(" "))
                {
                    commandSequence.Last().Add("");
                }

                previousCommandSequence = commandSequence;

                // nothing to autocomplete
                if (commandSequence.Count() == 0 || commandSequence.Last().Count == 0)
                {
                    SuggestedWords.Add("");
                    return;
                }

                ParsedCommand command = commandSequence.Last();

                var matchingCommands = ConsoleLine.Commands.Where(c => c.Name.StartsWith(command[0], StringComparison.OrdinalIgnoreCase)).ToList();

                // only one word in current command - autocomplete from all available commands
                if (command.Count() == 1)
                {
                    SuggestedWords.AddRange(matchingCommands.Select(c => c.Name).ToList());
                }
                // more than one words - get a command-specific autocompletion
                else if(matchingCommands.Count > 0)
                {
                    var cmdAutocomplete = matchingCommands.First().Autocomplete(command.Arguments);
                    if(cmdAutocomplete != null)
                    {
                        SuggestedWords.AddRange(cmdAutocomplete);
                    }
                }

                // make sure there's always at least one record
                if(SuggestedWords.Count() == 0)
                {
                    SuggestedWords.Add("");
                }

                int previousWordIndex = SuggestedWords.IndexOf(previousSuggestedWord);
                if (previousWordIndex >= 0) currentSuggestedWordIndex = previousWordIndex;
            }

            public void PreviousSuggestion()
            {
                currentSuggestedWordIndex--;
                if(currentSuggestedWordIndex < 0) currentSuggestedWordIndex = SuggestedWords.Count - 1;
            }

            public void NextSuggestion()
            {
                currentSuggestedWordIndex++;
                if(currentSuggestedWordIndex >= SuggestedWords.Count) currentSuggestedWordIndex = 0;
            }

            public string GetCurrentSuggestion()
            {
                string original = previousCommandSequence.Original;
                if (CurrentSuggestedWord == "") return original;
                string lastWord = previousCommandSequence.Last().Last();

                return original.Substring(0, original.Length - lastWord.Length) + CurrentSuggestedWord;
            }
        }

        public class CommandHandler
        {
            public List<IFezugCommand> Commands { get; private set; }

            private string _buffer;
            public string Buffer { get => _buffer; set
                {
                    _buffer = value;
                    OnBufferChanged();
                }
            }

            private bool bufferChangedByUser;
            private List<string> previousBufferInputs;

            public bool Enabled { get; set; }

            public AutocompletionManager Autocompletion { get; private set; }
            public bool ShouldAutocomplete => bufferChangedByUser;

            public event Action<ParsedCommandSequence> BufferChanged = delegate { };

            [ServiceDependency]
            public IInputManager InputManager { get; set; }

            [ServiceDependency(Optional = true)]
            public IKeyboardStateManager KeyboardState { private get; set; }

            public CommandHandler()
            {
                ServiceHelper.InjectServices(this);

                KeyboardState.RegisterKey(Keys.OemTilde);
                KeyboardState.RegisterKey(Keys.Enter);
                KeyboardState.RegisterKey(Keys.Escape);
                KeyboardState.RegisterKey(Keys.Tab);
                KeyboardState.RegisterKey(Keys.Up);
                KeyboardState.RegisterKey(Keys.Down);

                TextInputEXT.TextInput += OnTextInput;

                Commands = new List<IFezugCommand>();
                foreach (Type type in Assembly.GetExecutingAssembly().GetTypes()
                .Where(t => t.IsClass && typeof(IFezugCommand).IsAssignableFrom(t)))
                {
                    IFezugCommand command = (IFezugCommand)Activator.CreateInstance(type);
                    ServiceHelper.InjectServices(command);
                    Commands.Add(command);
                }

                Enabled = false;
                Buffer = "";

                previousBufferInputs = new List<string>();

                Autocompletion = new AutocompletionManager(this);
                BufferChanged += Autocompletion.Refresh;
            }

            private void OnBufferChanged()
            {
                BufferChanged.Invoke(new ParsedCommandSequence(Buffer));

                bufferChangedByUser = (Buffer.Length != 0);
            }

            public void OnTextInput(char c)
            {
                if (!Enabled) return;

                if (c == 0x08 && Buffer.Length > 0)
                {
                    Buffer = Buffer.Substring(0, Buffer.Length - 1);
                }
                else if (c >= 0x20 && c != '`' && DrawingTools.FontManager.Big.Characters.Contains(c))
                {
                    Buffer += c;
                }
            }

            public void ExecuteCommand(string commandString)
            {
                previousBufferInputs.Remove(commandString);
                previousBufferInputs.Add(commandString);

                var commands = new ParsedCommandSequence(commandString);

                foreach (var command in commands)
                {
                    string mainCommand = command.Command;
                    var args = command.Arguments;

                    var matchingCommands = Commands.Where(cmd => cmd.Name.Equals(command.Command, StringComparison.OrdinalIgnoreCase));

                    if (matchingCommands.Count() == 0)
                    {
                        FezugConsole.Print($"Unknown command: {mainCommand}", OutputType.Error);
                        continue;
                    }

                    bool successful = matchingCommands.First().Execute(args);
                }
            }

            public void Update(GameTime gameTime)
            {
                var Inputs = (InputManager)InputManager;

                // enable/disable console
                if (KeyboardState.GetKeyState(Keys.OemTilde) == FezButtonState.Pressed)
                {
                    Enabled = !Enabled;
                    Inputs.Enabled = !Enabled;
                }

                // we're manually blocking InputManager - make sure inputs are fetched properly
                if (!Inputs.Enabled)
                {
                    KeyboardState.Update(Keyboard.GetState(), gameTime);
                }

                if (!Enabled) return;

                // parse console-specific inputs
                if (KeyboardState.GetKeyState(Keys.Escape) == FezButtonState.Pressed)
                {
                    Buffer = "";
                }

                if (KeyboardState.GetKeyState(Keys.Enter) == FezButtonState.Pressed)
                {
                    Print($"> {Buffer}", new Color(128,128,128));
                    ExecuteCommand(Buffer);
                    Buffer = "";
                }

                if (KeyboardState.GetKeyState(Keys.Up) == FezButtonState.Pressed)
                {
                    if (!bufferChangedByUser && previousBufferInputs.Count > 0)
                    {
                        int index = Math.Max(0,previousBufferInputs.IndexOf(Buffer));
                        index = (index == 0 ? previousBufferInputs.Count : index) - 1;
                        Buffer = previousBufferInputs[index];
                        bufferChangedByUser = false;
                    }
                    else
                    {
                        Autocompletion.PreviousSuggestion();
                    }
                }

                if (KeyboardState.GetKeyState(Keys.Down) == FezButtonState.Pressed)
                {
                    if (!bufferChangedByUser && previousBufferInputs.Count > 0)
                    {
                        int index = (previousBufferInputs.IndexOf(Buffer) + 1) % previousBufferInputs.Count;
                        Buffer = previousBufferInputs[index];
                        bufferChangedByUser = false;
                    }
                    else
                    {
                        Autocompletion.NextSuggestion();
                    }
                }

                if (KeyboardState.GetKeyState(Keys.Tab) == FezButtonState.Pressed)
                {
                    Buffer = Autocompletion.GetCurrentSuggestion();
                }
            }
        }

        public enum OutputType
        {
            Info,
            Warning,
            Error,
        }
        private struct ConsoleOutput
        {
            public string Text;
            public Color Color;
        }

        private static readonly Dictionary<OutputType, Color> ConsoleOutputTypeColors = new Dictionary<OutputType, Color>()
        {
            {OutputType.Info, Color.White},
            {OutputType.Warning, Color.Yellow},
            {OutputType.Error, Color.IndianRed}
        };

        private List<ConsoleOutput> outputBuffer;
        private float blinkingTime;

        public CommandHandler Handler { get; private set; }
        public int OutputBufferLimit { get; set; }

        public static FezugConsole Instance { get; private set; }

        public FezugConsole()
        {
            Handler = new CommandHandler();
            Instance = this;

            outputBuffer = new List<ConsoleOutput>();
            OutputBufferLimit = 24;
        }


        public static void Clear()
        {
            Instance.outputBuffer.Clear();
        }

        public void Update(GameTime gameTime)
        {
            Handler.Update(gameTime);

            if (!Handler.Enabled) return;

            blinkingTime += (float)gameTime.ElapsedGameTime.TotalSeconds;

        }

        private void Print(ConsoleOutput output)
        {
            outputBuffer.Insert(0, output);

            while (outputBuffer.Count() > OutputBufferLimit)
            {
                outputBuffer.RemoveAt(outputBuffer.Count - 1);
            }
        }

        public static void Print(string text, Color color)
        {
            Instance.Print(new ConsoleOutput
            {
                Text = text,
                Color = color
            });
        }

        public static void Print(string text, OutputType type = OutputType.Info)
        {
            Print(text, ConsoleOutputTypeColors[type]);
        }

        public void Draw(GameTime gameTime)
        {
            if (!Handler.Enabled) return;

            Viewport viewport = DrawingTools.GetViewport();

            int margin = 20;
            int padding = 5;
            int lineHeight = 32;
            int outputBottomPadding = 50;

            // draw command line

            int commandY = viewport.Height - margin - lineHeight - padding * 2;
            int commandWidth = viewport.Width - margin * 2;

            DrawingTools.DrawRect(new Rectangle(margin, commandY, commandWidth, lineHeight + padding*2), new Color(10, 10, 10, 220));

            if (Handler.ShouldAutocomplete)
            {
                DrawingTools.DrawText(
                    $"> {Handler.Autocompletion.GetCurrentSuggestion()}",
                    new Vector2(margin + padding * 2, commandY - 5),
                    new Color(100, 100, 100)
                );
            }
            

            DrawingTools.DrawText(
                $"> {Handler.Buffer}{(blinkingTime % 1.0f > 0.5f ? "_" : "")}", 
                new Vector2(margin + padding * 2, commandY - 5),
                Color.White
            );

            // draw output field

            int outputWidth = commandWidth;
            int outputInnerWidth = commandWidth - padding * 4;
            int outputHeight = lineHeight * OutputBufferLimit + padding * 2;
            int outputY = commandY - outputBottomPadding - outputHeight;

            DrawingTools.DrawRect(new Rectangle(margin, outputY, outputWidth, outputHeight), new Color(10, 10, 10, 220));

            var outputItemPos = 0;
            foreach(var outputLine in outputBuffer)
            {
                List<string> lines = new List<string>();
                string lineBuffer = "";
                foreach(var word in outputLine.Text.Split(' '))
                {
                    float length = DrawingTools.DefaultFont.MeasureString(lineBuffer + word).X * DrawingTools.DefaultFontSize;
                    if(length > outputInnerWidth)
                    {
                        lines.Add(lineBuffer);
                        lineBuffer = $"{word} ";
                    }
                    else
                    {
                        lineBuffer += $"{word} ";
                    }
                }
                lines.Add(lineBuffer);

                for(int i= lines.Count - 1; i >= 0; i--)
                {
                    outputItemPos++;
                    DrawingTools.DrawText(
                        $"{lines[i]}",
                        new Vector2(margin + padding * 2, outputY + lineHeight * (OutputBufferLimit - outputItemPos) - 5),
                        outputLine.Color
                    );
                    if (outputItemPos >= OutputBufferLimit) break;
                }
                if (outputItemPos >= OutputBufferLimit) break;
            }
        }
    }
}
