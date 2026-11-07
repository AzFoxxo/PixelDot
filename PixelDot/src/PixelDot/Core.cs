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

using System.Diagnostics;
using System.Reflection;
using SFML.Graphics;
using SFML.System;
using SFML.Window;

namespace PixelDot;

public static class Core
{
    // Window
    public static RenderWindow? Window { get; private set; }

    // Currently executing application
    private static Application? currentApplication;

    // Pixel scale
    public static int PixelScale { get; private set; }

    // Background colour
    public static Color BackgroundColor { get; set; } = Palette.GetColour(0);

    // Screen width and height in logical pixels
    public static int ScreenWidth => (int)(Window?.Size.X ?? 0) / PixelScale;
    public static int ScreenHeight => (int)(Window?.Size.Y ?? 0) / PixelScale;

    // Mouse wheel scroll deltas — accumulated per frame, reset at frame start
    /// <summary>Vertical scroll delta this frame. Positive = scroll up, negative = scroll down.</summary>
    public static float ScrollDeltaY { get; private set; }
    /// <summary>Horizontal scroll delta this frame (trackpad / horizontal wheel).</summary>
    public static float ScrollDeltaX { get; private set; }

    // Target frame duration (60 FPS)
    private static readonly TimeSpan targetFrameTime = TimeSpan.FromSeconds(1.0 / 60.0);

    // Batch rendering vertex buffer — 6 vertices per logical pixel (two triangles, since SFML 3 removed Quads)
    private static Vertex[] vertexBuffer = new Vertex[1024];
    private static int vertexCount;

    /// <summary> Main entry point of the application </summary>
    /// <param name="args"> Arguments passed to the application </param>
    public static void Main(string[] args)
    {
        #region Argument Checking
        if (args.Length < 4)
        {
            Console.WriteLine("Not enough arguments provided\nUsage: PixelDot <width> <height> <scale> <application>");
            return;
        }

        var logicalWidth = int.Parse(args[0]);
        var logicalHeight = int.Parse(args[1]);

        PixelScale = Math.Max(1, int.Parse(args[2]));

        // Ensure maximum logical dimensions respect the desktop resolution
        var maxLogicalWidth = (int)VideoMode.DesktopMode.Size.X / PixelScale;
        var maxLogicalHeight = (int)VideoMode.DesktopMode.Size.Y / PixelScale;

        // Size restrictions in logical units
        if (logicalWidth < 100)
        {
            logicalWidth = 100;
        }
        if (logicalWidth > maxLogicalWidth)
        {
            logicalWidth = maxLogicalWidth;
        }

        if (logicalHeight < 50)
        {
            logicalHeight = 50;
        }
        if (logicalHeight > maxLogicalHeight)
        {
            logicalHeight = maxLogicalHeight;
        }
        #endregion

        #region Window Creation
        // Scale window dimensions by PixelScale and explicitly disable anti-aliasing
        var windowWidth = (uint)(logicalWidth * PixelScale);
        var windowHeight = (uint)(logicalHeight * PixelScale);
        Window = new RenderWindow(new VideoMode(new Vector2u(windowWidth, windowHeight)), "PixelDot", Styles.Titlebar | Styles.Close, State.Windowed, new ContextSettings { AntialiasingLevel = 0 });
        Window.Closed += (_, _) => Window.Close();

        // Accumulate mouse-wheel deltas into the per-frame scroll properties
        Window.MouseWheelScrolled += (_, e) =>
        {
            if (e.Wheel == Mouse.Wheel.Vertical)
                ScrollDeltaY += e.Delta;
            else
                ScrollDeltaX += e.Delta;
        };
        #endregion

        #region Initialise logging
        Logger.Setup(true, true, "log/", Logger.Levels.DEBUG);
        #endregion

        #region Find all applications with a RunnableApplication attribute
        var applications = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t => typeof(Application).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
            .ToList();

        var runnableApplications = applications
            .Where(t => t.GetCustomAttribute<RunnableApplicationAttribute>() is not null)
            .ToList();

        if (runnableApplications.Count == 0)
        {
            Logger.LogError("No applications with a RunnableApplication attribute found");
            return;
        }

        // Only search runnableApplications (classes with RunnableApplicationAttribute)
        var targetType = runnableApplications.FirstOrDefault(t => t.Name == args[3]);
        if (targetType is null)
        {
            Logger.LogError($"Application `{args[3]}` does not match any valid class name");
            Logger.Log("Acceptable applications:", Logger.Levels.INFO);

            foreach (var appType in runnableApplications)
            {
                var attr = appType.GetCustomAttribute<RunnableApplicationAttribute>()!;
                Logger.Log($"`{appType.Name}` - {attr.Name} ({attr.Version}) - {attr.Description}", Logger.Levels.INFO);
            }
            return;
        }

        currentApplication = Activator.CreateInstance(targetType) as Application;
        var appAttr = targetType.GetCustomAttribute<RunnableApplicationAttribute>();
        Window.SetTitle($"PixelDot - {appAttr?.Name ?? targetType.Name}");
        Logger.Log($"Running application {targetType.Name}", Logger.Levels.INFO);
        #endregion

        #region Run Application Lifecycle
        try
        {
            currentApplication?.OnInit();

            var stopwatch = Stopwatch.StartNew();

            while (Window.IsOpen)
            {
                stopwatch.Restart();

                // Reset per-frame scroll accumulation before collecting new events
                ScrollDeltaY = 0f;
                ScrollDeltaX = 0f;

                Window.DispatchEvents();

                if (!Window.IsOpen)
                {
                    break;
                }

                Window.Clear(BackgroundColor);
                currentApplication?.OnUpdate();
                RenderBatch();
                Window.Display();

                var elapsed = stopwatch.Elapsed;
                if (elapsed < targetFrameTime)
                {
                    Thread.Sleep(targetFrameTime - elapsed);
                }
            }
        }
        finally
        {
            currentApplication?.OnEnd();
            Audio.Dispose();
            Window?.Close();
            Window?.Dispose();
        }
        #endregion
    }

    /// <summary> Flushes and draws all batched pixels to the window </summary>
    public static void RenderBatch()
    {
        if (vertexCount > 0 && Window is not null)
        {
            Window.Draw(vertexBuffer, 0, (uint)vertexCount, PrimitiveType.Triangles);
            vertexCount = 0;
        }
    }

    /// <summary> Draw pixel on screen </summary>
    /// <param name="x"> X position of pixel </param>
    /// <param name="y"> Y position of pixel </param>
    /// <param name="color"> Color of pixel </param>
    /// <param name="width"> Width of pixel </param>
    /// <param name="height"> Height of pixel </param>
    public static void DrawPixel(int x, int y, Color color, int width = 1, int height = 1)
    {
        if (width <= 0 || height <= 0)
        {
            return;
        }

        if (vertexCount + 6 > vertexBuffer.Length)
        {
            Array.Resize(ref vertexBuffer, Math.Max(vertexBuffer.Length * 2, vertexCount + 6));
        }

        var px = x * PixelScale;
        var py = y * PixelScale;
        var pw = width * PixelScale;
        var ph = height * PixelScale;

        // Two triangles (6 vertices) replace the former Quads primitive (4 vertices)
        var tl = new Vector2f(px, py);
        var tr = new Vector2f(px + pw, py);
        var br = new Vector2f(px + pw, py + ph);
        var bl = new Vector2f(px, py + ph);

        vertexBuffer[vertexCount++] = new Vertex(tl, color);
        vertexBuffer[vertexCount++] = new Vertex(tr, color);
        vertexBuffer[vertexCount++] = new Vertex(br, color);

        vertexBuffer[vertexCount++] = new Vertex(br, color);
        vertexBuffer[vertexCount++] = new Vertex(bl, color);
        vertexBuffer[vertexCount++] = new Vertex(tl, color);
    }

    /// <summary> Draw line on screen </summary>
    /// <param name="x1"> X position of first point </param>
    /// <param name="y1"> Y position of first point </param>
    /// <param name="x2"> X position of second point </param>
    /// <param name="y2"> Y position of second point </param>
    /// <param name="color"> Color of line </param>
    /// <param name="thickness"> Thickness of line </param>
    public static void DrawLine(int x1, int y1, int x2, int y2, Color color, int thickness = 1)
    {
        if (thickness <= 0)
        {
            return;
        }

        var dx = x2 - x1;
        var dy = y2 - y1;
        var steps = Math.Max(Math.Abs(dx), Math.Abs(dy));
        if (steps == 0)
        {
            DrawPixel(x1, y1, color, thickness, thickness);
            return;
        }

        var xIncrement = (float)dx / steps;
        var yIncrement = (float)dy / steps;
        var x = (float)x1;
        var y = (float)y1;

        for (var i = 0; i <= steps; i++)
        {
            DrawPixel((int)Math.Round(x), (int)Math.Round(y), color, thickness, thickness);
            x += xIncrement;
            y += yIncrement;
        }
    }

    /// <summary> Draw circle on screen </summary>
    /// <param name="x"> X position of center </param>
    /// <param name="y"> Y position of center </param>
    /// <param name="radius"> Radius of circle </param>
    /// <param name="color"> Color of circle </param>
    /// <param name="thickness"> Thickness of circle </param>
    /// <param name="filled"> Whether the circle should be filled </param>
    public static void DrawCircle(int x, int y, int radius, Color color, int thickness = 1, bool filled = false)
    {
        if (radius < 0 || thickness <= 0)
        {
            return;
        }

        if (radius == 0)
        {
            DrawPixel(x, y, color, 1, 1);
            return;
        }

        if (filled || thickness >= radius)
        {
            for (var dy = -radius; dy <= radius; dy++)
            {
                var dx = (int)Math.Round(Math.Sqrt(radius * radius - dy * dy));
                DrawPixel(x - dx, y + dy, color, (2 * dx) + 1, 1);
            }
            return;
        }

        var innerRadius = radius - thickness;
        for (var dy = -radius; dy <= radius; dy++)
        {
            var dxOuter = (int)Math.Round(Math.Sqrt(radius * radius - dy * dy));

            if (Math.Abs(dy) <= innerRadius)
            {
                var dxInner = (int)Math.Round(Math.Sqrt(innerRadius * innerRadius - dy * dy));
                if (dxInner >= dxOuter)
                {
                    dxInner = dxOuter - 1;
                }

                var segWidth = dxOuter - dxInner;
                if (segWidth > 0)
                {
                    DrawPixel(x - dxOuter, y + dy, color, segWidth, 1);
                    DrawPixel(x + dxInner + 1, y + dy, color, segWidth, 1);
                }
            }
            else
            {
                DrawPixel(x - dxOuter, y + dy, color, (2 * dxOuter) + 1, 1);
            }
        }
    }

    /// <summary> Draw rectangle on screen </summary>
    /// <param name="x"> X position of top left corner </param>
    /// <param name="y"> Y position of top left corner </param>
    /// <param name="width"> Width of rectangle </param>
    /// <param name="height"> Height of rectangle </param>
    /// <param name="color"> Color of rectangle </param>
    /// <param name="thickness"> Thickness of rectangle </param>
    /// <param name="filled"> Whether the rectangle should be filled </param>
    public static void DrawRectangle(int x, int y, int width, int height, Color color, int thickness = 1, bool filled = false)
    {
        if (width <= 0 || height <= 0 || thickness <= 0)
        {
            return;
        }

        // When filled or when thickness covers the entire interior, draw one solid block
        if (filled || thickness * 2 >= width || thickness * 2 >= height)
        {
            DrawPixel(x, y, color, width, height);
            return;
        }

        // Draw outline without duplicate overlapping vertices:
        // Top bar
        DrawPixel(x, y, color, width, thickness);

        // Bottom bar
        DrawPixel(x, y + height - thickness, color, width, thickness);

        // Left bar (fills height between top and bottom bars)
        DrawPixel(x, y + thickness, color, thickness, height - (thickness * 2));

        // Right bar (fills height between top and bottom bars)
        DrawPixel(x + width - thickness, y + thickness, color, thickness, height - (thickness * 2));
    }

    /// <summary>
    /// Draw triangle on screen
    /// </summary>
    /// <param name="x1">Point one X</param>
    /// <param name="y1">Point one Y</param>
    /// <param name="x2">Point two X</param>
    /// <param name="y2">Point two Y</param>
    /// <param name="x3">Point three X</param>
    /// <param name="y3">Point three Y</param>
    /// <param name="color">Color of triangle</param>
    /// <param name="filled">Whether the triangle should be filled</param>
    public static void DrawTriangle(int x1, int y1, int x2, int y2, int x3, int y3, Color color, bool filled = false)
    {
        if (filled)
        {
            // Use barycentric coordinates to fill the triangle
            var minX = Math.Min(x1, Math.Min(x2, x3));
            var maxX = Math.Max(x1, Math.Max(x2, x3));
            var minY = Math.Min(y1, Math.Min(y2, y3));
            var maxY = Math.Max(y1, Math.Max(y2, y3));

            for (var y = minY; y <= maxY; y++)
            {
                for (var x = minX; x <= maxX; x++)
                {
                    var w0 = ((y2 - y3) * (x - x3) + (x3 - x2) * (y - y3)) / ((y2 - y3) * (x1 - x3) + (x3 - x2) * (y1 - y3));
                    var w1 = ((y3 - y1) * (x - x3) + (x1 - x3) * (y - y3)) / ((y2 - y3) * (x1 - x3) + (x3 - x2) * (y1 - y3));
                    var w2 = 1.0f - w0 - w1;

                    if (w0 >= 0 && w1 >= 0 && w2 >= 0)
                    {
                        DrawPixel(x, y, color);
                    }
                }
            }
        }
        else
        {
            // Draw triangle outline using lines
            DrawLine(x1, y1, x2, y2, color);
            DrawLine(x2, y2, x3, y3, color);
            DrawLine(x3, y3, x1, y1, color);
        }
    }
}
