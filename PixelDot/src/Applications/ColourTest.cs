/*
 *   Copyright (c) 2023 Az Foxxo (@AzFoxxo on GitHub)
 *   All rights reserved.

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

namespace UserCode
{
    [CurrentApplication("Colour Test", "1.0.0", "Az Foxxo", "Display all the colours", true)]
    public class ColourTest : Application
    {
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