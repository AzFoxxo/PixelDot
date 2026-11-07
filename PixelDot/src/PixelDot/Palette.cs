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
/// Palette layers supported by PixelDot.
/// </summary>
public enum PaletteLayer
{
    GUI,
    Background,
    Foreground,
    Sprite,
    FX
}

/// <summary>
/// Palette class is responsible for managing colour palettes and palette layers in PixelDot.
/// </summary>
public static class Palette
{
    /// <summary>
    /// Default colour palette is https://lospec.com/palette-list/nes-advanced
    /// </summary>
    public static readonly Color[] DefaultColours =
    [
        new(0x26, 0x23, 0x2f),
        new(0x31, 0x40, 0x47),
        new(0x59, 0x6d, 0x62),
        new(0x92, 0x9c, 0x74),
        new(0xc8, 0xc5, 0xa3),
        new(0xfc, 0xfc, 0xfc),
        new(0x1b, 0x37, 0x7f),
        new(0x14, 0x7a, 0xbf),
        new(0x40, 0xaf, 0xdd),
        new(0xb2, 0xdb, 0xf4),
        new(0x18, 0x16, 0x67),
        new(0x3b, 0x2c, 0x96),
        new(0x70, 0x6a, 0xe1),
        new(0x8f, 0x95, 0xee),
        new(0x44, 0x0a, 0x41),
        new(0x81, 0x25, 0x93),
        new(0xcc, 0x4b, 0xb9),
        new(0xec, 0x99, 0xdb),
        new(0x3f, 0x00, 0x11),
        new(0xb3, 0x1c, 0x35),
        new(0xef, 0x20, 0x64),
        new(0xf2, 0x62, 0x82),
        new(0x96, 0x08, 0x11),
        new(0xe8, 0x18, 0x13),
        new(0xa7, 0x5d, 0x69),
        new(0xec, 0x9e, 0xa4),
        new(0x56, 0x0d, 0x04),
        new(0xc4, 0x36, 0x11),
        new(0xe2, 0x6a, 0x12),
        new(0xf0, 0xaf, 0x66),
        new(0x2a, 0x1a, 0x14),
        new(0x5d, 0x34, 0x2a),
        new(0xa6, 0x6e, 0x46),
        new(0xdf, 0x9c, 0x6e),
        new(0x8e, 0x4e, 0x11),
        new(0xd8, 0x95, 0x11),
        new(0xea, 0xd1, 0x1e),
        new(0xf5, 0xeb, 0x6b),
        new(0x2f, 0x54, 0x1c),
        new(0x5a, 0x83, 0x1b),
        new(0xa2, 0xbb, 0x1e),
        new(0xc6, 0xdf, 0x6b),
        new(0x0f, 0x45, 0x0f),
        new(0x00, 0x8b, 0x12),
        new(0x0b, 0xcb, 0x12),
        new(0x3e, 0xf3, 0x3f),
        new(0x11, 0x51, 0x53),
        new(0x0c, 0x85, 0x63),
        new(0x04, 0xbf, 0x79),
        new(0x6a, 0xe6, 0xaa),
        new(0x26, 0x27, 0x26),
        new(0x51, 0x4f, 0x4c),
        new(0x88, 0x7e, 0x83),
        new(0xb3, 0xaa, 0xc0)
    ];

    private static readonly Dictionary<PaletteLayer, Color[]> _layers = new()
    {
        [PaletteLayer.GUI] = (Color[])DefaultColours.Clone(),
        [PaletteLayer.Background] = (Color[])DefaultColours.Clone(),
        [PaletteLayer.Foreground] = (Color[])DefaultColours.Clone(),
        [PaletteLayer.Sprite] = (Color[])DefaultColours.Clone(),
        [PaletteLayer.FX] = (Color[])DefaultColours.Clone(),
    };

    /// <summary>
    /// Gets or sets the currently active palette layer.
    /// </summary>
    public static PaletteLayer ActiveLayer { get; set; } = PaletteLayer.GUI;

    /// <summary>
    /// Sets the currently active palette layer.
    /// </summary>
    /// <param name="layer">Palette layer to set as active</param>
    public static void SetLayer(PaletteLayer layer) => ActiveLayer = layer;

    /// <summary>
    /// Gets the currently active palette layer.
    /// </summary>
    public static PaletteLayer GetLayer() => ActiveLayer;

    /// <summary>
    /// All supported colours in the active palette layer.
    /// </summary>
    public static Color[] Colours
    {
        get => _layers[ActiveLayer];
        set
        {
            if (value is null || value.Length == 0)
            {
                throw new ArgumentException("Palette colours cannot be null or empty.", nameof(value));
            }
            _layers[ActiveLayer] = value;
        }
    }

    /// <summary>
    /// Gets the array of colours for a specific palette layer.
    /// </summary>
    public static Color[] GetColours(PaletteLayer layer) => _layers[layer];

    /// <summary>
    /// Sets the array of colours for a specific palette layer.
    /// </summary>
    public static void SetColours(PaletteLayer layer, Color[] colours)
    {
        if (colours is null || colours.Length == 0)
        {
            throw new ArgumentException("Palette colours cannot be null or empty.", nameof(colours));
        }
        _layers[layer] = colours;
    }

    /// <summary>
    /// Get a colour from the active palette layer (wraps around if index is out of range).
    /// </summary>
    public static Color GetColour(byte index) => GetColour(index, ActiveLayer);

    /// <summary>
    /// Get a colour from the specified palette layer (wraps around if index is out of range).
    /// </summary>
    public static Color GetColour(byte index, PaletteLayer layer)
    {
        var colours = _layers[layer];
        return colours[index % colours.Length];
    }

    /// <summary>
    /// Resets the specified palette layer to the default colour palette.
    /// </summary>
    public static void ResetLayer(PaletteLayer layer)
    {
        _layers[layer] = (Color[])DefaultColours.Clone();
    }

    /// <summary>
    /// Resets all palette layers to the default colour palette.
    /// </summary>
    public static void ResetAllLayers()
    {
        foreach (var layer in Enum.GetValues<PaletteLayer>())
        {
            _layers[layer] = (Color[])DefaultColours.Clone();
        }
    }

    /// <summary>
    /// Load custom colour palette from a file into a specific layer, or the active layer if omitted.
    /// </summary>
    /// <param name="path">Filepath to palette</param>
    /// <param name="layer">Target layer, or null to load into the currently active layer</param>
    /// <exception cref="FileNotFoundException">Raised if the file does not exist</exception>
    /// <exception cref="FormatException">Raised if the file is not in a valid format</exception>
    public static void LoadFromFile(string path, PaletteLayer? layer = null)
    {
        var targetLayer = layer ?? ActiveLayer;
        var loadedColours = ParsePaletteFile(path);
        _layers[targetLayer] = loadedColours;
    }

    /// <summary>
    /// Load custom colour palette from a file into the specified palette layer.
    /// </summary>
    /// <param name="layer">Target palette layer</param>
    /// <param name="path">Filepath to palette</param>
    public static void LoadLayerFromFile(PaletteLayer layer, string path) =>
        LoadFromFile(path, layer);

    /// <summary>
    /// Parses a palette file from disk into a Color array.
    /// </summary>
    /// <param name="path">Filepath to palette</param>
    /// <returns>Array of parsed Colors</returns>
    public static Color[] ParsePaletteFile(string path)
    {
        if (!File.Exists(path)) throw new FileNotFoundException($"Palette file not found: {path}");

        var lines = File.ReadAllLines(path);
        List<Color> colours = [];

        foreach (var line in lines)
        {
            string trimmed = line.Trim();

            if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("#"))
            {
                continue;
            }

            // Hex format (RRGGBB or #RRGGBB)
            if (!trimmed.Contains(','))
            {
                string hex = trimmed.TrimStart('#');

                if (hex.Length != 6 ||
                    !uint.TryParse(hex,
                        System.Globalization.NumberStyles.HexNumber,
                        null, out uint value))
                {
                    throw new FormatException(
                        $"Invalid hex colour in palette file: {line}");
                }

                byte r = (byte)((value >> 16) & 0xFF);
                byte g = (byte)((value >> 8) & 0xFF);
                byte b = (byte)(value & 0xFF);

                colours.Add(new Color(r, g, b));
            }
            // RGB format (R, G, B)
            else
            {
                string[] parts = trimmed.Split(',');

                if (parts.Length != 3)
                {
                    throw new FormatException(
                        $"Invalid colour format in palette file: {line}");
                }

                byte r = byte.Parse(parts[0].Trim());
                byte g = byte.Parse(parts[1].Trim());
                byte b = byte.Parse(parts[2].Trim());

                colours.Add(new Color(r, g, b));
            }
        }

        if (colours.Count == 0) throw new FormatException("Palette file contains no colours.");

        return [.. colours];
    }
}
