using FEZUG.Features.Console;
using FEZUG.Helpers;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace FEZUG.Features.Hud
{
    internal class HudPositioner
    {
        public FezugVariable XCoordVariable { get; private set; }
        public FezugVariable YCoordVariable { get; private set; }

        public HudPositioner(string name, string helpText, float defaultX, float defaultY)
        {
            XCoordVariable = new FezugVariable(
                $"{name}_hud_position_x", 
                $"Changes the X position of {helpText} HUD (value between 0 and 1)",
                defaultX.ToString()
            )
            {
                SaveOnChange = true,
                Min = 0, 
                Max = 1
            };

            YCoordVariable = new FezugVariable(
                $"{name}_hud_position_y", 
                $"Changes the Y position of {helpText} HUD (value between 0 and 1)",
                defaultY.ToString()
            )
            {
                SaveOnChange = true,
                Min = 0, 
                Max = 1
            };
        }

        public Vector2 GetPosition(float width, float height)
        {
            return new Vector2(
                (DrawingTools.GetViewport().Width - width) * XCoordVariable.ValueFloat,
                (DrawingTools.GetViewport().Height - height) * YCoordVariable.ValueFloat
            );
        }
    }
}
