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
    // Gamepad-style abstractions
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

    // Raw keyboard keys (prefixed with Keyboard<name>)
    // Letters
    KeyboardA = Key.A,
    KeyboardB = Key.B,
    KeyboardC = Key.C,
    KeyboardD = Key.D,
    KeyboardE = Key.E,
    KeyboardF = Key.F,
    KeyboardG = Key.G,
    KeyboardH = Key.H,
    KeyboardI = Key.I,
    KeyboardJ = Key.J,
    KeyboardK = Key.K,
    KeyboardL = Key.L,
    KeyboardM = Key.M,
    KeyboardN = Key.N,
    KeyboardO = Key.O,
    KeyboardP = Key.P,
    KeyboardQ = Key.Q,
    KeyboardR = Key.R,
    KeyboardS = Key.S,
    KeyboardT = Key.T,
    KeyboardU = Key.U,
    KeyboardV = Key.V,
    KeyboardW = Key.W,
    KeyboardX = Key.X,
    KeyboardY = Key.Y,
    KeyboardZ = Key.Z,

    // Number row
    Keyboard0 = Key.Num0,
    Keyboard1 = Key.Num1,
    Keyboard2 = Key.Num2,
    Keyboard3 = Key.Num3,
    Keyboard4 = Key.Num4,
    Keyboard5 = Key.Num5,
    Keyboard6 = Key.Num6,
    Keyboard7 = Key.Num7,
    Keyboard8 = Key.Num8,
    Keyboard9 = Key.Num9,
    KeyboardNum0 = Key.Num0,
    KeyboardNum1 = Key.Num1,
    KeyboardNum2 = Key.Num2,
    KeyboardNum3 = Key.Num3,
    KeyboardNum4 = Key.Num4,
    KeyboardNum5 = Key.Num5,
    KeyboardNum6 = Key.Num6,
    KeyboardNum7 = Key.Num7,
    KeyboardNum8 = Key.Num8,
    KeyboardNum9 = Key.Num9,

    // Function keys
    KeyboardF1 = Key.F1,
    KeyboardF2 = Key.F2,
    KeyboardF3 = Key.F3,
    KeyboardF4 = Key.F4,
    KeyboardF5 = Key.F5,
    KeyboardF6 = Key.F6,
    KeyboardF7 = Key.F7,
    KeyboardF8 = Key.F8,
    KeyboardF9 = Key.F9,
    KeyboardF10 = Key.F10,
    KeyboardF11 = Key.F11,
    KeyboardF12 = Key.F12,
    KeyboardF13 = Key.F13,
    KeyboardF14 = Key.F14,
    KeyboardF15 = Key.F15,

    // Navigation and editing
    KeyboardEscape = Key.Escape,
    KeyboardSpace = Key.Space,
    KeyboardEnter = Key.Enter,
    KeyboardBackspace = Key.Backspace,
    KeyboardTab = Key.Tab,
    KeyboardPageUp = Key.PageUp,
    KeyboardPageDown = Key.PageDown,
    KeyboardEnd = Key.End,
    KeyboardHome = Key.Home,
    KeyboardInsert = Key.Insert,
    KeyboardDelete = Key.Delete,
    KeyboardPause = Key.Pause,

    // Arrow keys
    KeyboardLeft = Key.Left,
    KeyboardRight = Key.Right,
    KeyboardUp = Key.Up,
    KeyboardDown = Key.Down,
    KeyboardArrowLeft = Key.Left,
    KeyboardArrowRight = Key.Right,
    KeyboardArrowUp = Key.Up,
    KeyboardArrowDown = Key.Down,

    // Modifiers & System
    KeyboardLControl = Key.LControl,
    KeyboardRControl = Key.RControl,
    KeyboardLShift = Key.LShift,
    KeyboardRShift = Key.RShift,
    KeyboardLAlt = Key.LAlt,
    KeyboardRAlt = Key.RAlt,
    KeyboardLSystem = Key.LSystem,
    KeyboardRSystem = Key.RSystem,
    KeyboardMenu = Key.Menu,

    // Symbols & Punctuation
    KeyboardTilde = Key.Grave,
    KeyboardGrave = Key.Grave,
    KeyboardHyphen = Key.Hyphen,
    KeyboardMinus = Key.Hyphen,
    KeyboardEqual = Key.Equal,
    KeyboardEquals = Key.Equal,
    KeyboardLBracket = Key.LBracket,
    KeyboardRBracket = Key.RBracket,
    KeyboardSemicolon = Key.Semicolon,
    KeyboardApostrophe = Key.Apostrophe,
    KeyboardQuote = Key.Apostrophe,
    KeyboardComma = Key.Comma,
    KeyboardPeriod = Key.Period,
    KeyboardSlash = Key.Slash,
    KeyboardBackslash = Key.Backslash,

    // Keypad / Numpad
    KeyboardNumpad0 = Key.Numpad0,
    KeyboardNumpad1 = Key.Numpad1,
    KeyboardNumpad2 = Key.Numpad2,
    KeyboardNumpad3 = Key.Numpad3,
    KeyboardNumpad4 = Key.Numpad4,
    KeyboardNumpad5 = Key.Numpad5,
    KeyboardNumpad6 = Key.Numpad6,
    KeyboardNumpad7 = Key.Numpad7,
    KeyboardNumpad8 = Key.Numpad8,
    KeyboardNumpad9 = Key.Numpad9,
    KeyboardNumpadAdd = Key.Add,
    KeyboardNumpadSubtract = Key.Subtract,
    KeyboardNumpadMultiply = Key.Multiply,
    KeyboardNumpadDivide = Key.Divide,
    KeyboardAdd = Key.Add,
    KeyboardSubtract = Key.Subtract,
    KeyboardMultiply = Key.Multiply,
    KeyboardDivide = Key.Divide,

    // Legacy raw keyboard aliases
    ArrowLeft = Key.Left,
    ArrowRight = Key.Right,
    ArrowUp = Key.Up,
    ArrowDown = Key.Down,
    Space = Key.Space,
    Enter = Key.Enter,
    Z = Key.Z,
    X = Key.X,
    Q = Key.Q,
    R = Key.R,
    M = Key.M,
    P = Key.P,
    LShift = Key.LShift,
    RShift = Key.RShift,
    Tab = Key.Tab,
    L = Key.L,
    Num1 = Key.Num1,
    Num2 = Key.Num2,
    Num3 = Key.Num3,
    Num4 = Key.Num4,
    Num5 = Key.Num5,
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

    /// <summary>
    /// Vertical mouse-wheel scroll delta accumulated this frame.
    /// Positive = scrolled up / away from user, negative = scrolled down / towards user.
    /// Returns 0 when the wheel was not moved this frame.
    /// </summary>
    public static float GetScrollDelta() => Core.ScrollDeltaY;

    /// <summary>
    /// Horizontal mouse-wheel scroll delta accumulated this frame (trackpad or horizontal wheel).
    /// Positive = scrolled right, negative = scrolled left.
    /// Returns 0 when the wheel was not moved this frame.
    /// </summary>
    public static float GetHorizontalScrollDelta() => Core.ScrollDeltaX;

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
