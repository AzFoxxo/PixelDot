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
/// A 2D collider that performs pixel-perfect collision detection using an independent binary collision mask.
/// </summary>
public class PixelDataCollider
{
    private static int _nextId;

    private bool[] _mask = [];

    #region Properties

    /// <summary>
    /// Gets or sets the unique identifier for this collider.
    /// </summary>
    public int Id { get; set; } = Interlocked.Increment(ref _nextId);

    /// <summary>
    /// Gets or sets the user-friendly name of the collider.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the X position of the collider in world space.
    /// </summary>
    public int X { get; set; }

    /// <summary>
    /// Gets or sets the Y position of the collider in world space.
    /// </summary>
    public int Y { get; set; }

    /// <summary>
    /// Gets or sets an optional horizontal position offset from <see cref="X"/>.
    /// </summary>
    public int OffsetX { get; set; }

    /// <summary>
    /// Gets or sets an optional vertical position offset from <see cref="Y"/>.
    /// </summary>
    public int OffsetY { get; set; }

    /// <summary>
    /// Gets the effective world X coordinate including <see cref="OffsetX"/>.
    /// </summary>
    public int WorldX => X + OffsetX;

    /// <summary>
    /// Gets the effective world Y coordinate including <see cref="OffsetY"/>.
    /// </summary>
    public int WorldY => Y + OffsetY;

    /// <summary>
    /// Gets the width of the collision mask in pixels.
    /// </summary>
    public int Width { get; private set; }

    /// <summary>
    /// Gets the height of the collision mask in pixels.
    /// </summary>
    public int Height { get; private set; }

    /// <summary>
    /// Gets or sets whether this collider participates in collision detection.
    /// When disabled, it will not register collisions and will exit any active collisions.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets whether this collider acts as a trigger.
    /// Triggers detect overlap and fire trigger events without causing physical collision response.
    /// </summary>
    public bool IsTrigger { get; set; }

    /// <summary>
    /// Gets or sets the collision layer this collider belongs to.
    /// </summary>
    public CollisionLayer Layer { get; set; } = CollisionLayer.All;

    /// <summary>
    /// Gets or sets the collision mask specifying which layers this collider can interact with.
    /// </summary>
    public CollisionLayer CollisionMask { get; set; } = CollisionLayer.All;

    /// <summary>
    /// Gets the rectangular world bounding box for broad-phase collision testing.
    /// </summary>
    public CollisionRect Bounds => new(WorldX, WorldY, Width, Height);

    /// <summary>
    /// Gets the internal boolean collision mask (true = solid, false = transparent).
    /// </summary>
    public IReadOnlyList<bool> CollisionMaskData => _mask;

    /// <summary>
    /// User-defined object reference associated with this collider (e.g. parent game entity).
    /// </summary>
    public object? Tag { get; set; }

    #endregion

    #region Events

    /// <summary>Fired when a physical collision begins with this collider.</summary>
    public event Action<CollisionResult>? OnCollisionEnter;

    /// <summary>Fired each update while a physical collision continues with this collider.</summary>
    public event Action<CollisionResult>? OnCollisionStay;

    /// <summary>Fired when a physical collision ends with this collider.</summary>
    public event Action<CollisionResult>? OnCollisionExit;

    /// <summary>Fired when a trigger overlap begins with this collider.</summary>
    public event Action<CollisionResult>? OnTriggerEnter;

    /// <summary>Fired each update while a trigger overlap continues with this collider.</summary>
    public event Action<CollisionResult>? OnTriggerStay;

    /// <summary>Fired when a trigger overlap ends with this collider.</summary>
    public event Action<CollisionResult>? OnTriggerExit;

    #endregion

    #region Constructors

    /// <summary>
    /// Initialises a new instance of <see cref="PixelDataCollider"/>.
    /// </summary>
    public PixelDataCollider()
    {
    }

    /// <summary>
    /// Initialises a new instance of <see cref="PixelDataCollider"/> with a name.
    /// </summary>
    public PixelDataCollider(string name) : this()
    {
        Name = name;
    }

    /// <summary>
    /// Initialises a new instance of <see cref="PixelDataCollider"/> with a name and initial position.
    /// </summary>
    public PixelDataCollider(string name, int x, int y) : this(name)
    {
        X = x;
        Y = y;
    }

    /// <summary>
    /// Initialises a new instance of <see cref="PixelDataCollider"/> with name, position, and pixel data.
    /// </summary>
    public PixelDataCollider(string name, int x, int y, PixelData data) : this(name, x, y)
    {
        LoadPixelData(data);
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Loads pixel data into this collider, constructing a solid collision mask from non-transparent pixels.
    /// </summary>
    /// <param name="data">The pixel data to build the mask from.</param>
    public void LoadPixelData(PixelData data)
    {
        if (data is null || data.Width == 0 || data.Height == 0)
        {
            Clear();
            return;
        }

        Width = data.Width;
        Height = data.Height;
        _mask = data.GetSolidMask();
    }

    /// <summary>
    /// Loads pixel data into this collider using an explicitly configured transparent colour.
    /// </summary>
    /// <param name="data">The pixel data to build the mask from.</param>
    /// <param name="transparentColor">The colour that should be treated as transparent/empty space.</param>
    public void LoadPixelData(PixelData data, Color transparentColor)
    {
        if (data is null || data.Width == 0 || data.Height == 0)
        {
            Clear();
            return;
        }

        Width = data.Width;
        Height = data.Height;
        _mask = data.GetSolidMask(transparentColor);
    }

    /// <summary>
    /// Loads a pre-built boolean collision mask directly.
    /// </summary>
    /// <param name="mask">1D boolean array of length <paramref name="width"/> * <paramref name="height"/>.</param>
    /// <param name="width">Width in pixels.</param>
    /// <param name="height">Height in pixels.</param>
    public void LoadMask(bool[] mask, int width, int height)
    {
        ArgumentNullException.ThrowIfNull(mask);
        if (width < 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height < 0) throw new ArgumentOutOfRangeException(nameof(height));

        if (mask.Length != width * height)
        {
            throw new ArgumentException($"Mask length ({mask.Length}) does not match width * height ({width * height}).", nameof(mask));
        }

        Width = width;
        Height = height;
        _mask = (bool[])mask.Clone();
    }

    /// <summary>
    /// Clears the collider's collision mask and resets width and height to zero.
    /// </summary>
    public void Clear()
    {
        Width = 0;
        Height = 0;
        _mask = [];
    }

    /// <summary>
    /// Checks if a local pixel coordinate inside this collider is solid.
    /// </summary>
    /// <param name="localX">X position relative to this collider's top-left.</param>
    /// <param name="localY">Y position relative to this collider's top-left.</param>
    /// <returns>True if the pixel is solid, false if transparent or out of bounds.</returns>
    public bool IsSolid(int localX, int localY)
    {
        if (localX < 0 || localX >= Width || localY < 0 || localY >= Height || _mask.Length == 0)
        {
            return false;
        }

        return _mask[localY * Width + localX];
    }

    /// <summary>
    /// Checks if a world coordinate point overlaps a solid pixel of this collider.
    /// </summary>
    /// <param name="worldX">World space X coordinate.</param>
    /// <param name="worldY">World space Y coordinate.</param>
    /// <returns>True if the world position hits a solid pixel of this collider.</returns>
    public bool IsSolidWorld(int worldX, int worldY)
    {
        return IsSolid(worldX - WorldX, worldY - WorldY);
    }

    /// <summary>
    /// Broad phase check: checks whether the rectangular bounding boxes of two colliders overlap.
    /// </summary>
    /// <param name="other">The other collider to test against.</param>
    /// <returns>True if the rectangular bounds overlap, otherwise false.</returns>
    public bool CheckBounds(PixelDataCollider other)
    {
        if (other is null || !Enabled || !other.Enabled)
        {
            return false;
        }

        if (Width <= 0 || Height <= 0 || other.Width <= 0 || other.Height <= 0)
        {
            return false;
        }

        return WorldX < other.WorldX + other.Width &&
               WorldX + Width > other.WorldX &&
               WorldY < other.WorldY + other.Height &&
               WorldY + Height > other.WorldY;
    }

    /// <summary>
    /// Performs a two-stage collision test (broad-phase bounds check followed by narrow-phase pixel check).
    /// </summary>
    /// <param name="other">The other collider to test against.</param>
    /// <returns>A <see cref="CollisionResult"/> containing overlap details if a collision occurred, or null if no collision.</returns>
    public CollisionResult? CheckCollision(PixelDataCollider other)
    {
        if (other is null || !Enabled || !other.Enabled)
        {
            return null;
        }

        if (ReferenceEquals(this, other))
        {
            return null;
        }

        // Broad phase
        if (!CheckBounds(other))
        {
            return null;
        }

        // Compute intersection bounds in world space
        var overlapLeft = Math.Max(WorldX, other.WorldX);
        var overlapTop = Math.Max(WorldY, other.WorldY);
        var overlapRight = Math.Min(WorldX + Width, other.WorldX + other.Width);
        var overlapBottom = Math.Min(WorldY + Height, other.WorldY + other.Height);

        var overlapW = overlapRight - overlapLeft;
        var overlapH = overlapBottom - overlapTop;

        if (overlapW <= 0 || overlapH <= 0)
        {
            return null;
        }

        // Narrow phase: scan overlapping pixels
        var overlappingPixels = 0;

        for (var y = overlapTop; y < overlapBottom; y++)
        {
            var aRowStart = (y - WorldY) * Width;
            var bRowStart = (y - other.WorldY) * other.Width;
            var aColStart = overlapLeft - WorldX;
            var bColStart = overlapLeft - other.WorldX;

            for (var x = 0; x < overlapW; x++)
            {
                if (_mask[aRowStart + aColStart + x] && other._mask[bRowStart + bColStart + x])
                {
                    overlappingPixels++;
                }
            }
        }

        if (overlappingPixels == 0)
        {
            return null;
        }

        var bounds = new CollisionRect(overlapLeft, overlapTop, overlapW, overlapH);
        return new CollisionResult(this, other, bounds, overlappingPixels);
    }

    #endregion

    #region Internal Event Dispatchers

    internal void RaiseCollisionEnter(CollisionResult result) => OnCollisionEnter?.Invoke(result);
    internal void RaiseCollisionStay(CollisionResult result) => OnCollisionStay?.Invoke(result);
    internal void RaiseCollisionExit(CollisionResult result) => OnCollisionExit?.Invoke(result);
    internal void RaiseTriggerEnter(CollisionResult result) => OnTriggerEnter?.Invoke(result);
    internal void RaiseTriggerStay(CollisionResult result) => OnTriggerStay?.Invoke(result);
    internal void RaiseTriggerExit(CollisionResult result) => OnTriggerExit?.Invoke(result);

    #endregion

    public override string ToString() =>
        $"PixelDataCollider [Id={Id}, Name=\"{Name}\", Pos=({WorldX},{WorldY}), Size={Width}x{Height}, Enabled={Enabled}, Trigger={IsTrigger}, Layer={Layer}]";
}
