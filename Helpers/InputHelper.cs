using FezEngine.Services;
using FezEngine.Tools;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FEZUG.Helpers
{
    internal static class InputHelper
    {
        public static KeyboardState CurrentKeyboardState { get; private set; }
        public static KeyboardState PreviousKeyboardState { get; private set; }

        public static void Update()
        {
            PreviousKeyboardState = CurrentKeyboardState;
            CurrentKeyboardState = Keyboard.GetState();

        }

        public static bool IsKeyPressed(Keys key)
        {
            return PreviousKeyboardState.IsKeyUp(key) && CurrentKeyboardState.IsKeyDown(key);
        }

        public static bool IsKeyHeld(Keys key)
        {
            return CurrentKeyboardState.IsKeyDown(key);
        }

        public static bool IsKeyReleased(Keys key)
        {
            return PreviousKeyboardState.IsKeyDown(key) && CurrentKeyboardState.IsKeyUp(key);
        }
    }
}
