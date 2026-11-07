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
/// Contains detailed information about a detected collision between two colliders.
/// </summary>
public sealed class CollisionResult
{
    /// <summary>
    /// Gets the first collider in the collision pair.
    /// </summary>
    public PixelDataCollider ColliderA { get; }

    /// <summary>
    /// Gets the second collider in the collision pair.
    /// </summary>
    public PixelDataCollider ColliderB { get; }

    /// <summary>
    /// Gets whether this collision involves at least one trigger collider.
    /// </summary>
    public bool IsTrigger => ColliderA.IsTrigger || ColliderB.IsTrigger;

    /// <summary>
    /// Gets the axis-aligned world bounds of the overlapping collision area.
    /// </summary>
    public CollisionRect OverlapBounds { get; }

    /// <summary>
    /// Gets the total count of solid pixels that overlap between the two colliders.
    /// </summary>
    public int OverlappingPixelCount { get; }

    /// <summary>
    /// Gets the approximate contact center X in world coordinates.
    /// </summary>
    public int ContactPointX => OverlapBounds.X + (OverlapBounds.Width / 2);

    /// <summary>
    /// Gets the approximate contact center Y in world coordinates.
    /// </summary>
    public int ContactPointY => OverlapBounds.Y + (OverlapBounds.Height / 2);

    /// <summary>
    /// Initialises a new instance of <see cref="CollisionResult"/>.
    /// </summary>
    public CollisionResult(
        PixelDataCollider colliderA,
        PixelDataCollider colliderB,
        CollisionRect overlapBounds,
        int overlappingPixelCount)
    {
        ColliderA = colliderA ?? throw new ArgumentNullException(nameof(colliderA));
        ColliderB = colliderB ?? throw new ArgumentNullException(nameof(colliderB));
        OverlapBounds = overlapBounds;
        OverlappingPixelCount = overlappingPixelCount;
    }

    /// <summary>
    /// Given one of the colliders in this pair, returns the other collider.
    /// </summary>
    /// <param name="current">One of the colliders in this collision.</param>
    /// <returns>The opposing collider.</returns>
    /// <exception cref="ArgumentException">Thrown if <paramref name="current"/> is neither ColliderA nor ColliderB.</exception>
    public PixelDataCollider GetOther(PixelDataCollider current)
    {
        if (ReferenceEquals(current, ColliderA)) return ColliderB;
        if (ReferenceEquals(current, ColliderB)) return ColliderA;
        throw new ArgumentException("Specified collider is not part of this collision result.", nameof(current));
    }

    public override string ToString() =>
        $"Collision [{ColliderA.Name} <-> {ColliderB.Name}, Trigger={IsTrigger}, OverlapPixels={OverlappingPixelCount}, Bounds={OverlapBounds}]";
}
