/*
 *   Copyright (c) 2026 Az Foxxo (@AzFoxxo on GitHub)

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

namespace Applications;

[RunnableApplication("Test", "1.0.0", "Az Foxxo", "Example application for testing PixelDot")]
public class Test : Application
{
    private bool trigger;

    public override void OnEnd()
    {
        Logger.Log("Game ended");
    }

    public override void OnInit()
    {
        Logger.Log("Game initialised");

        // Set the font
        Text.SetFont("Regular");
        Text.SetSize(10);
        Text.SetColor(Palette.GetColour(5));

        // Load the sample and play the sample at channel 1
        // var sample = Audio.LoadSample("example_audio");
        // Audio.Play(1, sample, true);
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

        // Draw triangle at 10, 10, 20, 10, 15, 20 with the palette color 7
        Core.DrawTriangle(10, 10, 20, 10, 15, 20, Palette.GetColour(7));

        // Test text rendering
        Text.Draw(10, 10, "Hello, World!");

        // Log mouse position to console (x, y)
        Logger.Log($"Mouse position: {Input.GetMousePositionX()}, {Input.GetMousePositionY()}");

        if (Input.IsPressed(MouseButtons.Left))
        {
            trigger = true;
        }
        else if (Input.IsReleased(MouseButtons.Left))
        {
            trigger = false;
        }

        if (trigger)
        {
            byte offset = 0;
            for (byte x = 0; x < Core.ScreenWidth; x++)
            {
                for (byte y = 0; y < Core.ScreenHeight; y++)
                {
                    Core.DrawPixel(x, y, Palette.GetColour((byte)(x + offset)), 1);
                    offset++;
                }

                if (offset > 58)
                {
                    offset = 0;
                }
            }
        }
    }
}
