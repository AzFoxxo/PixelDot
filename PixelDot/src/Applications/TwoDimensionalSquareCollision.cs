/*
 *   Copyright (c) 2026 Az Foxxo (@AzFoxxo on GitHub)
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

// Coordinates
public readonly record struct Points(float x, float y);

// Square representation
public readonly record struct Square(Points position, Points size, SFML.Graphics.Color colour)
{
    /// <summary>
    /// Return if the square is overlapping another square
    /// </summary>
    public bool IsOverlapping(Square other) =>
        position.x < other.position.x + other.size.x &&
        position.x + size.x > other.position.x &&
        position.y < other.position.y + other.size.y &&
        position.y + size.y > other.position.y;
}

[RunnableApplication("Collision Test", "1.0.0", "Az Foxxo", "2D collision testing")]
public class TwoDimensionalSquareCollision : Application
{
    private Square[] objects = [];

    public override void OnInit()
    {
        objects =
        [
            new(new Points(10, 10), new Points(2, 2), Palette.GetColour(5)),
            new(new Points(10, 11), new Points(2, 2), Palette.GetColour(6))
        ];
    }

    public override void OnUpdate()
    {
        foreach (var obj in objects)
        {
            Core.DrawPixel((int)obj.position.x, (int)obj.position.y, obj.colour, (int)obj.size.x, (int)obj.size.y);
        }

        Logger.Log($"Overlapping: {objects[0].IsOverlapping(objects[1])}");
    }
}
