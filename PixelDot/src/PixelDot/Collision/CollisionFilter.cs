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
/// Helper class for collision layer filtering and interaction rules.
/// </summary>
public static class CollisionFilter
{
    /// <summary>
    /// Determines whether two colliders are allowed to interact based on their layers and collision masks.
    /// Both colliders must permit interaction with the other collider's layer.
    /// </summary>
    /// <param name="a">First collider.</param>
    /// <param name="b">Second collider.</param>
    /// <returns>True if both colliders allow interaction with each other, otherwise false.</returns>
    public static bool ShouldCollide(PixelDataCollider a, PixelDataCollider b)
    {
        if (a is null || b is null)
        {
            return false;
        }

        return ShouldCollide(a.Layer, a.CollisionMask, b.Layer, b.CollisionMask);
    }

    /// <summary>
    /// Determines whether two collision layer/mask combinations allow interaction.
    /// </summary>
    /// <param name="layerA">Layer of collider A.</param>
    /// <param name="maskA">Collision mask of collider A.</param>
    /// <param name="layerB">Layer of collider B.</param>
    /// <param name="maskB">Collision mask of collider B.</param>
    /// <returns>True if interaction is permitted, otherwise false.</returns>
    public static bool ShouldCollide(
        CollisionLayer layerA, CollisionLayer maskA,
        CollisionLayer layerB, CollisionLayer maskB)
    {
        return (maskA & layerB) != 0 && (maskB & layerA) != 0;
    }

    /// <summary>
    /// Checks whether a layer is included within a collision mask.
    /// </summary>
    public static bool Matches(CollisionLayer layer, CollisionLayer mask) =>
        (mask & layer) != 0;
}
