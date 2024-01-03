/*
 *   Copyright (c) 2023 Az Foxxo (@AzFoxxo on GitHub)

 *   Permission is hereby granted, free of charge, to any person obtaining a copy
 *   of this software and associated documentation files (the "Software"), to deal
 *   in the Software without restriction, including without limitation the rights
 *   to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 *   copies of the Software, and to permit persons to whom the Software is
 *   furnished to do so, subject to the following conditions:
 
 *   The above copyright notice and this permission notice shall be included in all
 *   copies or substantial portions of the Software.
 
 *   THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 *   IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 *   FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 *   AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 *   LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 *   OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 *   SOFTWARE.
 */

using PixelDot;

// Example application for testing PixelDot
namespace UserCode
{
    [CurrentApplication("Test", "1.0.0", "Az Foxxo", "Example application for testing PixelDot", false)]
    public class Test : Application
    {
        private bool trigger = false;
        public override void OnEnd()
        {
            Logger.Log("Game ended");
        }

        public override void OnInit()
        {
            Logger.Log("Game initialised");
        }

        public override void OnUpdate()
        {
            // Draw a circle at half width, half height with a radius of 10 and the palette color 6
            Core.DrawCircle(Core.ScreenWidth / 2, Core.ScreenHeight / 2, 10, Palette.GetColour(6));

            // Draw a circle at half width, half height with a radius of 5 and the palette color 8 and filled
            Core.DrawCircle(Core.ScreenWidth / 2, Core.ScreenHeight / 2, 5, Palette.GetColour(8), 1, true);

            // Draw a single pixel at 0, 0 with the palette color 0
            Core.DrawPixel(0, 0, Palette.GetColour(0));

            // Draw a single line from 0, 0 to ScreenWidth, ScreenHeight
            Core.DrawLine(0, 0, Core.ScreenWidth, Core.ScreenHeight, Palette.GetColour(5), 1);

            // Log mouse position to console (x, y)
            Logger.Log($"Mouse position: {Input.GetMousePositionX()}, {Input.GetMousePositionY()}");

            // If pressed A, set AAAA to true
            if (Input.IsPressed(MouseButtons.Left))
                trigger = true;
            else if (Input.IsReleased(MouseButtons.Left))
                trigger = false;

            if (trigger)
            {
                // Draw all 58 palette colours and then repeat
                byte offset = 0;
                // Loop over each pixel horizontally
                for (byte x = 0; x < Core.ScreenWidth; x++)
                {
                    // Loop over each pixel vertically
                    for (byte y = 0; y < Core.ScreenHeight; y++)
                    {
                        // Draw a pixel at x, y with the palette color x
                        Core.DrawPixel(x, y, Palette.GetColour((byte)(x + offset)), 1);

                        // Increment the offset
                        offset++;
                    }

                    // If the offset is greater than 58, reset it to 0
                    if (offset > 58)
                        offset = 0;
                }
            }
        }
    }
}