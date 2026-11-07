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

namespace PixelDot.Collision;

/// <summary>
/// Represents an integer 2D bounding rectangle used for collision detection.
/// </summary>
public readonly record struct CollisionRect(int X, int Y, int Width, int Height)
{
    /// <summary>
    /// Gets the left X coordinate of the rectangle.
    /// </summary>
    public int Left => X;

    /// <summary>
    /// Gets the top Y coordinate of the rectangle.
    /// </summary>
    public int Top => Y;

    /// <summary>
    /// Gets the right X coordinate of the rectangle.
    /// </summary>
    public int Right => X + Width;

    /// <summary>
    /// Gets the bottom Y coordinate of the rectangle.
    /// </summary>
    public int Bottom => Y + Height;

    /// <summary>
    /// Returns true if this rectangle has zero or negative area.
    /// </summary>
    public bool IsEmpty => Width <= 0 || Height <= 0;

    /// <summary>
    /// Checks whether this rectangle overlaps another rectangle.
    /// </summary>
    public bool Intersects(CollisionRect other) =>
        X < other.X + other.Width &&
        X + Width > other.X &&
        Y < other.Y + other.Height &&
        Y + Height > other.Y;

    /// <summary>
    /// Computes the intersection rectangle of two bounding boxes.
    /// </summary>
    public CollisionRect Intersect(CollisionRect other)
    {
        var x1 = Math.Max(X, other.X);
        var y1 = Math.Max(Y, other.Y);
        var x2 = Math.Min(X + Width, other.X + other.Width);
        var y2 = Math.Min(Y + Height, other.Y + other.Height);

        if (x2 <= x1 || y2 <= y1)
        {
            return new CollisionRect(0, 0, 0, 0);
        }

        return new CollisionRect(x1, y1, x2 - x1, y2 - y1);
    }

    /// <summary>
    /// Checks if a world coordinate point is inside this rectangle.
    /// </summary>
    public bool Contains(int px, int py) =>
        px >= X && px < X + Width &&
        py >= Y && py < Y + Height;

    public override string ToString() => $"[X={X}, Y={Y}, W={Width}, H={Height}]";
}
