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

namespace UserCode
{
    [CurrentApplication("Conway's Game of Life", "1.0.0", "Az Foxxo", "Implementation of Game of Life", false)]
    public class ConwaysGameOfLife : Application
    {
        // Grid
        private bool[,]? grid;

        // Customisable variables
        private const int INITIAL_CELLS = 250;
        
        // Init
        public override void OnInit() {
            grid = new bool[Core.ScreenWidth, Core.ScreenWidth];
            for (int x = 0; x < Core.ScreenWidth; x++)
            {
                for (int y = 0; y < Core.ScreenWidth; y++)
                {
                    grid[x, y] = false;
                }
            }

            // Set cells to be alive randomly
            Random random = new();
            for (int i = 0; i < INITIAL_CELLS; i++)
            { 
                grid[random.Next(0, Core.ScreenWidth), random.Next(0, Core.ScreenWidth)] = true;
               
            }
        }

        // Tick
        public override void OnUpdate() {
            for (int x = 0; x < Core.ScreenWidth; x++)
            {
                for (int y = 0; y < Core.ScreenWidth; y++)
                {
                    // Update the cell
                    UpdateCell(x, y);

                    // Draw the cell
                    if (grid![x, y])
                        Core.DrawPixel(x, y, Palette.GetColour(15));
                }
            }
        }

        // Update the cell
        private void UpdateCell(int x, int y)
        {
            var neighbours = 0;

            // Check all eight directions
            if (x > 0 && y > 0 && grid![x - 1, y - 1]) neighbours++;
            if (x > 0 && grid![x - 1, y]) neighbours++;
            if (x > 0 && y < Core.ScreenWidth - 1 && grid![x - 1, y + 1]) neighbours++;
            if (y > 0 && grid![x, y - 1]) neighbours++;
            if (y < Core.ScreenWidth - 1 && grid![x, y + 1]) neighbours++;
            if (x < Core.ScreenWidth - 1 && y > 0 && grid![x + 1, y - 1]) neighbours++;
            if (x < Core.ScreenWidth - 1 && grid![x + 1, y]) neighbours++;
            if (x < Core.ScreenWidth - 1 && y < Core.ScreenWidth - 1 && grid![x + 1, y + 1]) neighbours++;

            if (neighbours < 2 || neighbours > 3) grid![x, y] = false;
            else if (neighbours == 3) grid![x, y] = true; 
        }
    }
}
