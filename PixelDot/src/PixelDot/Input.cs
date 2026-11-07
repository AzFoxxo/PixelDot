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

namespace PixelDot;

/// <summary>
/// Keys supported by PixelDot
/// </summary>
public enum Keys
{
    Jump = Key.Space,
    Left = Key.A,
    Right = Key.D,
    Up = Key.W,
    Down = Key.S,
    Escape = Key.Escape,
    Start = Key.Enter,
    Select = Key.Backspace,
    A = Key.Z,
    B = Key.X,
    C = Key.C,
    D = Key.V,
    Menu = Key.Tab,
}

// Mouse buttons
public enum MouseButtons
{
    Left = 0,
    Right = 1,
    Middle = 2,
}

/// <summary>
/// Key struct used to check if a key is pressed, held, or released
/// </summary>
public readonly record struct KeyState(bool Pressed, bool Held, bool Released, bool None);

/// <summary>
/// Input helper class for PixelDot
/// </summary>
public static class Input
{
    public static bool IsPressed(Keys key) => SFML.Window.Keyboard.IsKeyPressed((Key)key);

    public static bool IsPressed(MouseButtons button) => SFML.Window.Mouse.IsButtonPressed((SFML.Window.Mouse.Button)button);

    public static bool IsHeld(Keys key) => SFML.Window.Keyboard.IsKeyPressed((Key)key);

    public static bool IsHeld(MouseButtons button) => SFML.Window.Mouse.IsButtonPressed((SFML.Window.Mouse.Button)button);

    public static bool IsReleased(Keys key) => !SFML.Window.Keyboard.IsKeyPressed((Key)key);

    public static bool IsReleased(MouseButtons button) => !SFML.Window.Mouse.IsButtonPressed((SFML.Window.Mouse.Button)button);

    public static KeyState GetKeyState(Keys key)
    {
        var pressed = IsPressed(key);
        return new KeyState(pressed, pressed, !pressed, false);
    }

    public static KeyState GetKeyState(MouseButtons button)
    {
        var pressed = IsPressed(button);
        return new KeyState(pressed, pressed, !pressed, false);
    }

    /// <summary>
    /// Gets the mouse position X relative to the window canvas area.
    /// </summary>
    public static int GetMousePositionX(bool factorInPixel = true)
    {
        if (Core.Window is null) return 0;

        var viewport = Core.Window.GetViewport(Core.Window.DefaultView);
        var mousePosX = SFML.Window.Mouse.GetPosition(Core.Window).X - viewport.Left;

        return factorInPixel ? mousePosX / Core.PixelScale : mousePosX;
    }

    /// <summary>
    /// Gets the mouse position Y relative to the window canvas area.
    /// </summary>
    public static int GetMousePositionY(bool factorInPixel = true)
    {
        if (Core.Window is null) return 0;

        var viewport = Core.Window.GetViewport(Core.Window.DefaultView);
        var mousePosY = SFML.Window.Mouse.GetPosition(Core.Window).Y - viewport.Top;

        return factorInPixel ? mousePosY / Core.PixelScale : mousePosY;
    }
}
