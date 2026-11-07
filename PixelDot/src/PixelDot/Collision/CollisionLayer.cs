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
/// Flags-based enum representing collision layers and filtering masks in the PixelDot collision system.
/// </summary>
[Flags]
public enum CollisionLayer
{
    /// <summary>No collision layers selected.</summary>
    None = 0,

    /// <summary>Player entity layer.</summary>
    Player = 1 << 0,

    /// <summary>Enemy entity layer.</summary>
    Enemy = 1 << 1,

    /// <summary>Static or dynamic environmental obstacles (walls, platforms).</summary>
    Environment = 1 << 2,

    /// <summary>Projectiles (bullets, spells, missiles).</summary>
    Projectile = 1 << 3,

    /// <summary>Non-physical trigger zones (pickups, checkpoints, damage areas).</summary>
    Trigger = 1 << 4,

    /// <summary>All collision layers selected.</summary>
    All = ~0
}
