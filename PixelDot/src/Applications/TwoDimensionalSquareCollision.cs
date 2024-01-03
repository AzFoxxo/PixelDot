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
using System.Collections.Generic;
using System.Collections;

namespace UserCode
{
    // Co-ordinates
    public struct Points
    {
        public float x { get; set; }
        public float y { get; set; }

        public Points(float x, float y)
        {
            this.x = x;
            this.y = y;
        }
    }

    // Square representation
    public struct Square
    {
        public Points position {get; set; }
        public Points size {get; set; }
        public SFML.Graphics.Color colour {get; set; }

        public Square(Points position, Points Size, SFML.Graphics.Color colour)
        {
            this.position = position;
            this.size = Size;
            this.colour = colour;
        }

        /// <summary>
        /// Return if the square is overlapping another square
        /// </summary>
        /// <param name="other">The other square to check for overlapping</param>
        /// <returns>Returns true if overlapping</returns>
        public bool IsOverlapping(Square other) => position.x < other.position.x + other.size.x && position.x + size.x > other.position.x && position.y < other.position.y + other.size.y && position.y + size.y > other.position.y;
    }

    [CurrentApplication("Collision Test", "1.0.0", "Az Foxxo", "2D collision testing", false)]
    public class TwoDimensionalSquareCollision : Application
    {
        // List of all objects in the scene
        private Square[] objects = Array.Empty<Square>();
        
        // Init
        public override void OnInit() {
            // Add squares to the objects array
            objects = new Square[] { new(new Points(10, 10), new Points(2, 2), Palette.GetColour(5)), new (new Points(10, 11), new Points(2, 2), Palette.GetColour(6)) };
        }

        // Tick
        public override void OnUpdate() {
            // Draw all objects
            foreach (var obj in objects)
                Core.DrawPixel((int)obj.position.x, (int)obj.position.y, obj.colour, (int)obj.size.x, (int)obj.size.y);
            

            // Log overlap
            Logger.Log($"Overlapping: {objects[0].IsOverlapping(objects[1])}");
        }
    }
}
