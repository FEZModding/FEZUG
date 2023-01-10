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
    public class ConsoleLine
    {
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

        public List<IConsoleCommand> Commands { get; private set; }

        private static List<ConsoleOutput> outputBuffer;
        private string commandBuffer;
        private List<string> typedCommandsBuffer = new List<string>();
        private List<string> autocompleteList = new List<string>();
        private int autocompleteIndex = 0;
        private int autocompleteUnfinishedLength = 0;
        private float blinkingTime;

        public bool Enabled { get; private set; }

        public static int OutputBufferLimit { get; set; }

        [ServiceDependency]
        public IInputManager InputManager { get; set; }

        [ServiceDependency(Optional = true)]
        public IKeyboardStateManager KeyboardState { private get; set; }

        [ServiceDependency]
        public IGameStateManager GameState { get; set; }


        public ConsoleLine()
        {
            ServiceHelper.InjectServices(this);

            KeyboardState.RegisterKey(Keys.OemTilde);
            KeyboardState.RegisterKey(Keys.Enter);
            KeyboardState.RegisterKey(Keys.Escape);
            KeyboardState.RegisterKey(Keys.Tab);
            KeyboardState.RegisterKey(Keys.Up);
            KeyboardState.RegisterKey(Keys.Down);

            TextInputEXT.TextInput += OnTextInput;

            Commands = new List<IConsoleCommand>();
            foreach (Type type in Assembly.GetExecutingAssembly().GetTypes()
            .Where(t => t.IsClass && typeof(IConsoleCommand).IsAssignableFrom(t)))
            {
                IConsoleCommand command = (IConsoleCommand)Activator.CreateInstance(type);
                ServiceHelper.InjectServices(command);
                Commands.Add(command);
            }

            outputBuffer = new List<ConsoleOutput>();

            Enabled = false;
            OutputBufferLimit = 24;
        }

        public void OnTextInput(char c)
        {
            if (!Enabled) return;

            int prevCmdBufLen = commandBuffer.Length;

            if (c == 0x08 && commandBuffer.Length > 0)
            {
                commandBuffer = commandBuffer.Substring(0, commandBuffer.Length - 1);
            }
            else if (c >= 0x20 && c != '`' && DrawingTools.FontManager.Big.Characters.Contains(c))
            {
                commandBuffer += c;
                blinkingTime = 0.0f;
            }

            if (prevCmdBufLen != commandBuffer.Length) RefreshAutocompletion();
        }

        private void RefreshAutocompletion()
        {
            string prevAutocomplete = "";
            if (autocompleteIndex < autocompleteList.Count) prevAutocomplete = autocompleteList[autocompleteIndex];

            autocompleteList.Clear();

            if (commandBuffer.Length == 0)
            {
                autocompleteList.AddRange(typedCommandsBuffer);
                autocompleteIndex = autocompleteList.Count;
                return;
            }

            var lastCommand = commandBuffer.Split(';').Last();

            if (lastCommand.Contains(' '))
            {
                // create autocomplete based on written command
                int spacePos = lastCommand.IndexOf(' ');

                string mainCommand = lastCommand.Substring(0, spacePos).ToLower();
                string param = lastCommand.Substring(spacePos + 1);

                var matchingCommands = Commands.Where(cmd => cmd.Name == mainCommand);

                if(matchingCommands.Count() > 0)
                {
                    var newAutocompleteList = matchingCommands.First().Autocomplete(param);
                    if (newAutocompleteList != null)
                    {
                        autocompleteList = newAutocompleteList;
                        autocompleteIndex = Math.Max(0, autocompleteList.FindIndex(s => s == prevAutocomplete));
                    }
                    autocompleteUnfinishedLength = param.Length + commandBuffer.Length - lastCommand.Length;
                }
            }
            else
            {
                // create autocomplete based on available commands
                autocompleteList = Commands.Select(cmd => cmd.Name).Where(name => name.StartsWith(lastCommand)).ToList();
                autocompleteIndex = Math.Max(0, autocompleteList.FindIndex(s => s == prevAutocomplete));
                autocompleteUnfinishedLength = commandBuffer.Length;
            }
        }

        public string GetAutocompletedBuffer()
        {
            if (autocompleteIndex >= autocompleteList.Count) return commandBuffer;
            string firstPart = "";
            if (commandBuffer.Length > 0)
            {
                firstPart = commandBuffer.Substring(0, commandBuffer.Length - autocompleteUnfinishedLength);
            }
            return firstPart + autocompleteList[autocompleteIndex];
        }

        public void SendCommand(string commandString)
        {
            if (commandString.Length == 0) return;

            if(typedCommandsBuffer.Count == 0 || typedCommandsBuffer.Last() != commandString)
            {
                typedCommandsBuffer.Add(commandString);
            }
            Print(new ConsoleOutput
            {
                Text = $"> {commandString}",
                Color = new Color(180, 180, 180)
            });

            string[] splitCommands = commandString.Split(';');

            foreach(var splitCommand in splitCommands)
            {
                string[] args = splitCommand.Split(' ').Where(s => s.Length > 0).ToArray();

                string mainCommand = args[0].ToLower();
                args = args.Skip(1).ToArray();

                var matchingCommands = Commands.Where(cmd => cmd.Name == mainCommand);

                if (matchingCommands.Count() == 0)
                {
                    Print($"Unknown command: {mainCommand}", OutputType.Error);
                    continue;
                }

                bool successful = matchingCommands.First().Execute(args);
            }
        }

        public void ClearOutput()
        {
            outputBuffer.Clear();
        }

        public void Update(GameTime gameTime)
        {
            var Inputs = (InputManager)InputManager;

            if (KeyboardState.GetKeyState(Keys.OemTilde) == FezButtonState.Pressed)
            {
                commandBuffer = "";
                Enabled = !Enabled;
                Inputs.Enabled = !Enabled;
            }

            // we're manually blocking InputManager - make sure inputs are fetched properly
            if (!Inputs.Enabled)
            {
                KeyboardState.Update(Keyboard.GetState(), gameTime);
            }


            if (!Enabled) return;

            blinkingTime += (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (KeyboardState.GetKeyState(Keys.Escape) == FezButtonState.Pressed)
            {
                commandBuffer = "";
                RefreshAutocompletion();
            }

            if (KeyboardState.GetKeyState(Keys.Enter) == FezButtonState.Pressed)
            {
                SendCommand(commandBuffer);
                commandBuffer = "";

                RefreshAutocompletion();
            }

            if (KeyboardState.GetKeyState(Keys.Up) == FezButtonState.Pressed)
            {
                autocompleteIndex -= 1;
                if (autocompleteIndex < 0) autocompleteIndex = Math.Max(0, autocompleteList.Count - 1);
            }

            if (KeyboardState.GetKeyState(Keys.Down) == FezButtonState.Pressed)
            {
                autocompleteIndex += 1;
                if (autocompleteIndex >= autocompleteList.Count) autocompleteIndex = 0;
            }

            if (KeyboardState.GetKeyState(Keys.Tab) == FezButtonState.Pressed)
            {
                commandBuffer = GetAutocompletedBuffer();
                RefreshAutocompletion();
            }
        }

        private void Print(ConsoleOutput output)
        {
            outputBuffer.Insert(0, output);

            while (outputBuffer.Count() > OutputBufferLimit)
            {
                outputBuffer.RemoveAt(outputBuffer.Count - 1);
            }
        }

        public void Print(string text, OutputType type = OutputType.Info)
        {
            Print(new ConsoleOutput
            {
                Text = text,
                Color = ConsoleOutputTypeColors[type]
            });
        }

        public void Draw(GameTime gameTime)
        {
            if (!Enabled) return;

            Viewport viewport = DrawingTools.GetViewport();

            int margin = 20;
            int padding = 5;
            int lineHeight = 32;
            int outputBottomPadding = 50;

            // draw command line

            int commandY = viewport.Height - margin - lineHeight - padding * 2;
            int commandWidth = viewport.Width - margin * 2;

            DrawingTools.DrawRect(new Rectangle(margin, commandY, commandWidth, lineHeight + padding*2), new Color(10, 10, 10, 220));

            DrawingTools.DrawText(
                $"> {GetAutocompletedBuffer()}",
                new Vector2(margin + padding * 2, commandY - 5),
                new Color(100, 100, 100)
            );

            DrawingTools.DrawText(
                $"> {commandBuffer}{(blinkingTime % 1.0f > 0.5f ? "_" : "")}", 
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
