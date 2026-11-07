/*
 *   Copyright (c) 2024 Az Foxxo (@AzFoxxo on GitHub)
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

namespace Applications;

[RunnableApplication("Conway's Game of Life", "1.1.0", "Az Foxxo", "Implementation of Game of Life")]
public class ConwaysGameOfLife : Application
{
    private bool[,]? grid;
    private int[]? gridAsArrayValues;
    private int startingCellCount;

    public override void OnInit()
    {
        startingCellCount = (Core.ScreenWidth * Core.ScreenWidth) / 20;

        grid = new bool[Core.ScreenWidth, Core.ScreenWidth];
        gridAsArrayValues = new int[Core.ScreenWidth * Core.ScreenWidth];

        Logger.LogInfo($"Screen Size {Core.ScreenWidth}x{Core.ScreenWidth}");
        Logger.LogInfo($"Initial Cell Count: {startingCellCount}");

        for (var x = 0; x < Core.ScreenWidth; x++)
        {
            for (var y = 0; y < Core.ScreenHeight; y++)
            {
                grid[x, y] = false;
            }
        }

        var random = new Random();
        for (var i = 0; i < startingCellCount; i++)
        {
            grid[random.Next(0, Core.ScreenWidth), random.Next(0, Core.ScreenHeight)] = true;
        }
    }

    public override void OnUpdate()
    {
        var iterationInArray = 0;
        for (var x = 0; x < Core.ScreenWidth; x++)
        {
            for (var y = 0; y < Core.ScreenHeight; y++)
            {
                var value = UpdateCell(x, y);
                gridAsArrayValues![iterationInArray] = value;

                if (value == 1)
                {
                    Core.DrawPixel(x, y, Palette.GetColour(43));
                }
                else if (value == 2)
                {
                    Core.DrawPixel(x, y, Palette.GetColour(44));
                }
                else if (value == 3)
                {
                    Core.DrawPixel(x, y, Palette.GetColour(45));
                }

                iterationInArray++;
            }
        }
    }

    private int UpdateCell(int x, int y)
    {
        var neighbours = 0;

        if (x > 0 && y > 0 && grid![x - 1, y - 1]) neighbours++;
        if (x > 0 && grid![x - 1, y]) neighbours++;
        if (x > 0 && y < Core.ScreenWidth - 1 && grid![x - 1, y + 1]) neighbours++;
        if (y > 0 && grid![x, y - 1]) neighbours++;
        if (y < Core.ScreenWidth - 1 && grid![x, y + 1]) neighbours++;
        if (x < Core.ScreenWidth - 1 && y > 0 && grid![x + 1, y - 1]) neighbours++;
        if (x < Core.ScreenWidth - 1 && grid![x + 1, y]) neighbours++;
        if (x < Core.ScreenWidth - 1 && y < Core.ScreenWidth - 1 && grid![x + 1, y + 1]) neighbours++;

        if (neighbours is < 2 or > 3)
        {
            grid![x, y] = false;
        }
        else if (neighbours == 3)
        {
            grid![x, y] = true;
        }

        return neighbours;
    }
}
