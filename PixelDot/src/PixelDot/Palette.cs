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

namespace PixelDot
{
    /// <summary> All supported colours </summary>
    public readonly struct Palette
    {
        /// <summary>
        /// Get a colour from the palette (wraps around if index is out of range)
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public static SFML.Graphics.Color GetColour(byte index) => Colours[index % Colours.Length];
        
        // Colour palette is https://lospec.com/palette-list/nes-advanced
        public static readonly SFML.Graphics.Color[] Colours =
            {
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
        };
    }
}
