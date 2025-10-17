using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace FEZUG.Helpers
{
    internal class InputHelper
    {
        private readonly Dictionary<Keys, double> KeyboardRepeatHeldTimers = [];
        private readonly List<Keys> KeyboardRepeatedPresses = [];

        public KeyboardState CurrentKeyboardState { get; private set; }
        public KeyboardState PreviousKeyboardState { get; private set; }

        public double KeyboardRepeatDelay { get; set; } = 0.4;
        public double KeyboardRepeatSpeed { get; set; } = 0.03;

        public void Update(GameTime gameTime)
        {
            PreviousKeyboardState = CurrentKeyboardState;
            CurrentKeyboardState = Keyboard.GetState();


            KeyboardRepeatedPresses.Clear();
            foreach (Keys key in CurrentKeyboardState.GetPressedKeys())
            {
                if (IsKeyPressed(key) || !KeyboardRepeatHeldTimers.ContainsKey(key))
                {
                    KeyboardRepeatHeldTimers[key] = 0.0f;
                }

                KeyboardRepeatHeldTimers[key] += gameTime.ElapsedGameTime.TotalSeconds;
                if (KeyboardRepeatHeldTimers[key] > KeyboardRepeatDelay + KeyboardRepeatSpeed)
                {
                    KeyboardRepeatHeldTimers[key] = KeyboardRepeatDelay;
                    KeyboardRepeatedPresses.Add(key);
                }
            }
        }

        public  bool IsKeyPressed(Keys key)
        {
            return PreviousKeyboardState.IsKeyUp(key) && CurrentKeyboardState.IsKeyDown(key);
        }

        public bool IsKeyHeld(Keys key)
        {
            return CurrentKeyboardState.IsKeyDown(key);
        }

        public bool IsKeyReleased(Keys key)
        {
            return PreviousKeyboardState.IsKeyDown(key) && CurrentKeyboardState.IsKeyUp(key);
        }

        public bool IsKeyTyped(Keys key)
        {
            return IsKeyPressed(key) || KeyboardRepeatedPresses.Contains(key);
        }


        [Flags]
        public enum KeyModifierState
        {
            None = 0,
            Shift = 1,
            Alt = 2,
            Control = 4,
            Ctrl = 4,
        }
        public KeyModifierState GetKeyModifierState()
        {
            KeyModifierState state = 0;
            if (IsKeyHeld(Keys.LeftShift) || IsKeyHeld(Keys.RightShift))
            {
                state |= KeyModifierState.Shift;
            }
            if (IsKeyHeld(Keys.LeftControl) || IsKeyHeld(Keys.RightControl))
            {
                state |= KeyModifierState.Control;
            }
            if (IsKeyHeld(Keys.LeftAlt) || IsKeyHeld(Keys.RightAlt))
            {
                state |= KeyModifierState.Alt;
            }
            return state;
        }
    }
}
