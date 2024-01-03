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

using Key = SFML.Window.Keyboard.Key;

namespace PixelDot
{
    /// <summary>
    ///    Keys supported by PixelDot
    /// </summary>

    public enum Keys
    {
        Jump = Key.Space,          // Jump
        Left = Key.A,              // Left
        Right = Key.D,             // Right
        Up = Key.W,                // Up
        Down = Key.S,              // Down
        Escape = Key.Escape,       // Escape
        Start = Key.Enter,         // Start
        Select = Key.Backspace,    // Select
        A = Key.Z,                 // A
        B = Key.X,                 // B
        C = Key.C,                 // C
        D = Key.V,                 // D
        Menu = Key.Tab,            // Menu
    }

    // Mouse buttons
    public enum MouseButtons
    {
        Left = 0,   // Left mouse button
        Right = 1,  // Right mouse button
        Middle = 2, // Middle (scroll wheel) mouse button
    }

    /// <summary>
    ///    Key struct used to check if a key is pressed, held, or released or none of the above
    /// </summary>
    public struct KeyState
    {
        public bool Pressed;
        public bool Held;
        public bool Released;
        public bool None;
    }

    /// <summary>
    ///    Keys input class for PixelDot
    /// </summary>
    public static class Input
    {
        /// <summary>
        ///    Checks if a key is pressed
        /// </summary>
        /// <param name="key">Key to check</param>
        /// <returns>True if the key is pressed</returns>
        public static bool IsPressed(Keys key)
        {
            return SFML.Window.Keyboard.IsKeyPressed((Key)key);
        }

        /// <summary>
        /// Check if a mouse button is pressed
        /// </summary>
        /// <param name="button">Mouse button to check</param>
        /// <returns>True if the mouse button is pressed</returns>
        public static bool IsPressed(MouseButtons button)
        {
            return SFML.Window.Mouse.IsButtonPressed((SFML.Window.Mouse.Button)button);
        }

        /// <summary>
        ///    Checks if a key is held
        /// </summary>
        /// <param name="key">Key to check</param>
        /// <returns>True if the key is held</returns>
        public static bool IsHeld(Keys key)
        {
            return SFML.Window.Keyboard.IsKeyPressed((Key)key);
        }

        /// <summary>
        /// Check if a mouse button is held
        /// </summary>
        /// <param name="button">Mouse button to check</param>
        /// <returns>True if the mouse button is held</returns>
        public static bool IsHeld(MouseButtons button)
        {
            return SFML.Window.Mouse.IsButtonPressed((SFML.Window.Mouse.Button)button);
        }

        /// <summary>
        ///    Checks if a key is released
        /// </summary>
        /// <param name="key">Key to check</param>
        /// <returns>True if the key is released</returns>
        public static bool IsReleased(Keys key)
        {
            return !SFML.Window.Keyboard.IsKeyPressed((Key)key);
        }

        /// <summary>
        /// Check if a mouse button is released
        /// </summary>
        /// <param name="button">Mouse button to check</param>
        /// <returns>True if the mouse button is released</returns>
        public static bool IsReleased(MouseButtons button)
        {
            return !SFML.Window.Mouse.IsButtonPressed((SFML.Window.Mouse.Button)button);
        }

        /// <summary>
        ///    Gets the state of a key
        /// </summary>
        /// <param name="key">Key to check</param>
        /// <returns>KeyState of the key</returns>
        public static KeyState GetKeyState(Keys key)
        {
            return new KeyState
            {
                Pressed = IsPressed(key),
                Held = IsHeld(key),
                Released = IsReleased(key),
            };
        }

        /// <summary>
        ///    Gets the state of a mouse button
        /// </summary>
        /// <param name="button">Mouse button to check</param>
        /// <returns>KeyState of the mouse button</returns>
        public static KeyState GetKeyState(MouseButtons button)
        {
            return new KeyState
            {
                Pressed = IsPressed(button),
                Held = IsHeld(button),
                Released = IsReleased(button),
            };
        }

        /// <summary>
        /// Gets the mouse position X relative to the window's canvas area.
        /// </summary>
        /// <returns>Mouse position X as an integer.</returns>
        public static int GetMousePositionX(bool factorInPixel = true)
        {
            SFML.Graphics.RenderWindow window = Core.Window!;

            // Retrieve the viewport (drawable area) of the window
            SFML.Graphics.IntRect viewport = window.GetViewport(window.DefaultView);

            int mousePosX = SFML.Window.Mouse.GetPosition(window).X - viewport.Left;

            if (factorInPixel)
            {
                mousePosX /= (int)Core.PixelScale;
            }

            return mousePosX;
        }

        /// <summary>
        /// Gets the mouse position Y relative to the window's canvas area.
        /// </summary>
        /// <returns>Mouse position Y as an integer.</returns>
        public static int GetMousePositionY(bool factorInPixel = true)
        {
            SFML.Graphics.RenderWindow window = Core.Window!;

            // Retrieve the viewport (drawable area) of the window
            SFML.Graphics.IntRect viewport = window.GetViewport(window.DefaultView);

            int mousePosY = SFML.Window.Mouse.GetPosition(window).Y - viewport.Top;

            if (factorInPixel)
            {
                mousePosY /= (int)Core.PixelScale;
            }

            return mousePosY;
        }

    }
}