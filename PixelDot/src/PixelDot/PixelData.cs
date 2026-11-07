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

namespace PixelDot;

/// <summary>
/// Represents a 2D grid of pixel color data independent of the rendering system.
/// Can be used for sprite assets, canvas buffers, and collider collision masks.
/// </summary>
public sealed class PixelData
{
    private readonly Color[] _pixels;

    /// <summary>
    /// Gets the width of the pixel data in pixels.
    /// </summary>
    public int Width { get; }

    /// <summary>
    /// Gets the height of the pixel data in pixels.
    /// </summary>
    public int Height { get; }

    /// <summary>
    /// Gets the total number of pixels.
    /// </summary>
    public int Length => _pixels.Length;

    /// <summary>
    /// Gets the raw array of pixel colors.
    /// </summary>
    public Color[] Pixels => _pixels;

    /// <summary>
    /// Gets or sets a pixel color at the specified local coordinates.
    /// Returns a transparent color (0, 0, 0, 0) if out of bounds.
    /// </summary>
    public Color this[int x, int y]
    {
        get => GetPixel(x, y);
        set => SetPixel(x, y, value);
    }

    /// <summary>
    /// Initialises a new empty (transparent) <see cref="PixelData"/> with the given dimensions.
    /// </summary>
    public PixelData(int width, int height)
    {
        if (width < 0) throw new ArgumentOutOfRangeException(nameof(width), "Width must be non-negative.");
        if (height < 0) throw new ArgumentOutOfRangeException(nameof(height), "Height must be non-negative.");

        Width = width;
        Height = height;
        _pixels = new Color[width * height];
    }

    /// <summary>
    /// Initialises a new <see cref="PixelData"/> filled with a single color.
    /// </summary>
    public PixelData(int width, int height, Color fillColor) : this(width, height)
    {
        if (fillColor != default)
        {
            Array.Fill(_pixels, fillColor);
        }
    }

    /// <summary>
    /// Initialises a new <see cref="PixelData"/> from an existing array of colors.
    /// </summary>
    public PixelData(int width, int height, Color[] pixels)
    {
        if (width < 0) throw new ArgumentOutOfRangeException(nameof(width), "Width must be non-negative.");
        if (height < 0) throw new ArgumentOutOfRangeException(nameof(height), "Height must be non-negative.");
        ArgumentNullException.ThrowIfNull(pixels);

        if (pixels.Length != width * height)
        {
            throw new ArgumentException($"Pixel array length ({pixels.Length}) does not match width * height ({width * height}).", nameof(pixels));
        }

        Width = width;
        Height = height;
        _pixels = (Color[])pixels.Clone();
    }

    /// <summary>
    /// Initialises a new <see cref="PixelData"/> from palette indices.
    /// </summary>
    /// <param name="width">Width in pixels.</param>
    /// <param name="height">Height in pixels.</param>
    /// <param name="paletteIndices">Palette indices where transparentIndex represents transparency.</param>
    /// <param name="layer">Palette layer to resolve colors from.</param>
    /// <param name="transparentIndex">Index representing transparent pixels (defaults to 0xFF).</param>
    public PixelData(int width, int height, byte[] paletteIndices, PaletteLayer layer = PaletteLayer.Sprite, byte transparentIndex = 0xFF)
        : this(width, height)
    {
        ArgumentNullException.ThrowIfNull(paletteIndices);
        if (paletteIndices.Length != width * height)
        {
            throw new ArgumentException($"Palette indices length ({paletteIndices.Length}) does not match width * height ({width * height}).", nameof(paletteIndices));
        }

        for (var i = 0; i < paletteIndices.Length; i++)
        {
            var idx = paletteIndices[i];
            _pixels[i] = idx == transparentIndex
                ? new Color(0, 0, 0, 0)
                : Palette.GetColour(idx, layer);
        }
    }

    /// <summary>
    /// Initialises a new <see cref="PixelData"/> from a boolean solid mask.
    /// </summary>
    public PixelData(int width, int height, bool[] solidMask, Color? solidColor = null, Color? transparentColor = null)
        : this(width, height)
    {
        ArgumentNullException.ThrowIfNull(solidMask);
        if (solidMask.Length != width * height)
        {
            throw new ArgumentException($"Solid mask length ({solidMask.Length}) does not match width * height ({width * height}).", nameof(solidMask));
        }

        var solid = solidColor ?? new Color(255, 255, 255, 255);
        var transparent = transparentColor ?? new Color(0, 0, 0, 0);

        for (var i = 0; i < solidMask.Length; i++)
        {
            _pixels[i] = solidMask[i] ? solid : transparent;
        }
    }

    /// <summary>
    /// Gets the color of the pixel at (x, y).
    /// </summary>
    public Color GetPixel(int x, int y)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height)
        {
            return new Color(0, 0, 0, 0);
        }
        return _pixels[y * Width + x];
    }

    /// <summary>
    /// Sets the color of the pixel at (x, y).
    /// </summary>
    public void SetPixel(int x, int y, Color color)
    {
        if (x >= 0 && x < Width && y >= 0 && y < Height)
        {
            _pixels[y * Width + x] = color;
        }
    }

    /// <summary>
    /// Checks whether the pixel at (x, y) is solid (non-transparent).
    /// </summary>
    /// <param name="x">Local X coordinate.</param>
    /// <param name="y">Local Y coordinate.</param>
    /// <param name="transparentColor">Optional explicit transparent color.</param>
    /// <returns>True if the pixel is solid, false if transparent or out of bounds.</returns>
    public bool IsSolid(int x, int y, Color? transparentColor = null)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height)
        {
            return false;
        }

        var pixel = _pixels[y * Width + x];
        if (transparentColor.HasValue)
        {
            return pixel != transparentColor.Value && pixel.A > 0;
        }
        return pixel.A > 0;
    }

    /// <summary>
    /// Generates a 1D boolean collision mask where true = solid and false = transparent.
    /// </summary>
    /// <param name="transparentColor">Optional explicitly configured transparent color.</param>
    /// <returns>A boolean array representing the collision mask.</returns>
    public bool[] GetSolidMask(Color? transparentColor = null)
    {
        var mask = new bool[Width * Height];
        for (var i = 0; i < _pixels.Length; i++)
        {
            var p = _pixels[i];
            if (transparentColor.HasValue)
            {
                mask[i] = p != transparentColor.Value && p.A > 0;
            }
            else
            {
                mask[i] = p.A > 0;
            }
        }
        return mask;
    }

    #region Static Factory Helpers

    /// <summary>
    /// Creates a new <see cref="PixelData"/> instance from a 2D boolean mask.
    /// </summary>
    public static PixelData FromMask(bool[,] mask, Color? solidColor = null, Color? transparentColor = null)
    {
        ArgumentNullException.ThrowIfNull(mask);
        var width = mask.GetLength(0);
        var height = mask.GetLength(1);
        var flatMask = new bool[width * height];

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                flatMask[y * width + x] = mask[x, y];
            }
        }

        return new PixelData(width, height, flatMask, solidColor, transparentColor);
    }

    /// <summary>
    /// Creates a solid filled rectangle of pixel data.
    /// </summary>
    public static PixelData CreateSolid(int width, int height, Color? color = null)
    {
        return new PixelData(width, height, color ?? new Color(255, 255, 255, 255));
    }

    /// <summary>
    /// Creates a <see cref="PixelData"/> instance from a 1D boolean mask.
    /// </summary>
    public static PixelData FromMask(bool[] mask, int width, int height, Color? solidColor = null, Color? transparentColor = null)
    {
        return new PixelData(width, height, mask, solidColor, transparentColor);
    }

    #endregion
}
