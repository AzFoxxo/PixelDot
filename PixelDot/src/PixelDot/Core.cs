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

// Imports
using SFML.Graphics;
using SFML.System;
using SFML.Window;

namespace PixelDot {
    public class Core {
        // Window
        public static RenderWindow? Window { get; private set; }

        // Currently executing application
        private static Application? currentApplication;

        // Pixel scale
        public static int PixelScale { get; private set;}

        // Background colour
        public static SFML.Graphics.Color BackgroundColor { get; set; } = Palette.GetColour(0);

        // Screen width and height
        public static int ScreenWidth => (int)Window!.Size.X / PixelScale;
        public static int ScreenHeight => (int)Window!.Size.Y / PixelScale;

        /// <summary> Main entry point of the application </summary>
        /// <param name="args"> Arguments passed to the application </param>
        /// <returns> Nothing </returns>
        public static void Main(string[] args) {
            #region Argument Checking
            // Check at least three args are provided
            if (args.Length < 3) {
                throw new System.Exception("Not enough arguments provided\nUsage: PixelDot.exe <width> <height> <scale>");
            }

            // First two args are width and height
            var width = int.Parse(args[0]);
            var height = int.Parse(args[1]);

            // Third arg is scale
            PixelScale = int.Parse(args[2]);
            PixelScale = PixelScale < 1 ? 1 : PixelScale;

            // Size restrictions
            if (width < 100) {
                width = 100;
            } else if (width > VideoMode.DesktopMode.Width) {
                width = (int)VideoMode.DesktopMode.Width;
            }
            if (height < 50) {
                height = 50;
            } else if (height > VideoMode.DesktopMode.Height) {
                height = (int)VideoMode.DesktopMode.Height;
            }
            #endregion

            #region Window Creation
            // Create the window
            Window = new RenderWindow(new VideoMode((uint) width, (uint) height), "PixelDot", Styles.Titlebar | Styles.Close);
            Window.Closed += (sender, e) => Window.Close();
            #endregion

            #region Initialise logging
            // Setup the logger
            Logger.Setup(true, true, "log/", Logger.Levels.DEBUG);
            #endregion

            #region Find all applications with a CurrentApplication attribute
            // Find all classes that inherit from Application
            var applications = System.AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(x => x.GetTypes())
                .Where(x => typeof(Application).IsAssignableFrom(x) && !x.IsInterface && !x.IsAbstract)
                .ToList();

            // Find all applications with a CurrentApplication attribute
            var currentApplications = applications.Where(x => x.GetCustomAttributes(typeof(CurrentApplicationAttribute), false).Length > 0).ToList();

            // If there are no applications with a CurrentApplication attribute, throw an exception
            if (currentApplications.Count == 0) {
                Logger.Log("No applications with a CurrentApplication attribute found", Logger.Levels.ERROR);
                throw new System.Exception("No applications with a CurrentApplication attribute found");
            }

            // Find all currentApplications which are set to run
            var runningApplications = currentApplications.Where(x => ((CurrentApplicationAttribute)x.GetCustomAttributes(typeof(CurrentApplicationAttribute), false)[0]).Run).ToList();

            // If there are no applications set to run, throw an exception
            if (runningApplications.Count == 0) {
                Logger.Log("No applications set to run", Logger.Levels.ERROR);
                throw new System.Exception("No applications set to run");
            }

            // If there are more than one applications set to run, throw an exception
            if (runningApplications.Count > 1) {
                Logger.Log("More than one application set to run", Logger.Levels.ERROR);
                throw new System.Exception("More than one application set to run");
            }

            // Set the current application
            currentApplication = System.Activator.CreateInstance(runningApplications[0]) as Application;

            // Set the title to the name of the current application (using the CurrentApplication attribute)
            Window.SetTitle($"PixelDot - {((CurrentApplicationAttribute)runningApplications[0].GetCustomAttributes(typeof(CurrentApplicationAttribute), false)[0]).Name}");

            // Log a message saying which application is running
            Logger.Log($"Running application {runningApplications[0].Name}", Logger.Levels.INFO);
        #endregion

        #region Init method for current application
        // Call the init method for the current application
        currentApplication?.OnInit();
        #endregion

            // Enter the main loop
            while (Window.IsOpen) {
                // Current time
                var time = System.DateTime.Now;

                // Process events
                Window.DispatchEvents();

                // Check if the window has been closed
                if (!Window.IsOpen) {
                    break;
                }

                // Clear screen
                Window.Clear(BackgroundColor);

                // Call the update method for the current application
                currentApplication?.OnUpdate();

                // Update the display
                Window.Display();

                // Check at least 60ms have passed since last frame
                var timePassed = System.DateTime.Now - time;
                if (timePassed.TotalMilliseconds < 16.666666666666666666666666666667) {
                    // Sleep for the remaining time
                    System.Threading.Thread.Sleep((int)(16.666666666666666666666666666667 - timePassed.TotalMilliseconds));
                }
            }
        }

        // Clean-up the application
        ~Core() {
            // Call the destroy method for the current application
            currentApplication?.OnEnd();

            // Close the window
            Window?.Close();

            // Dispose of the window
            Window?.Dispose();
        }

        /// <summary> Draw pixel on screen </summary>
        /// <param name="x"> X position of pixel </param>
        /// <param name="y"> Y position of pixel </param>
        /// <param name="color"> Color of pixel </param>
        /// <param name="width"> Width of pixel </param>
        /// <param name="height"> Height of pixel </param>
        public static void DrawPixel(int x, int y, SFML.Graphics.Color color, int width=1, int height=1) {
            // Draw the rectangle
            var rectangle = new RectangleShape(new Vector2f(width * PixelScale, height * PixelScale));
            rectangle.Position = new Vector2f(x * PixelScale, y * PixelScale);
            rectangle.FillColor = color;
            Window?.Draw(rectangle);
        }

        /// <summary> Draw line on screen </summary>
        /// <param name="x1"> X position of first point </param>
        /// <param name="y1"> Y position of first point </param>
        /// <param name="x2"> X position of second point </param>
        /// <param name="y2"> Y position of second point </param>
        /// <param name="color"> Color of line </param>
        /// <param name="thickness"> Thickness of line </param>
        public static void DrawLine(int x1, int y1, int x2, int y2, SFML.Graphics.Color color, int thickness=1) {
            // Use the DrawPixel function to draw the line
            // Note: this is extremely inefficient as each pixel is drawn separately
            var dx = x2 - x1;
            var dy = y2 - y1;
            var steps = System.Math.Abs(dx) > System.Math.Abs(dy) ? System.Math.Abs(dx) : System.Math.Abs(dy);
            var xIncrement = (float)dx / (float)steps;
            var yIncrement = (float)dy / (float)steps;
            var x = (float)x1;
            var y = (float)y1;
            for (var i = 0; i <= steps; i++) {
                DrawPixel((int)x, (int)y, color, thickness, thickness);
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
        /// <params name="filled"> Whether the circle should be filled </params>
        public static void DrawCircle(int x, int y, int radius, SFML.Graphics.Color color, int thickness=1, bool filled=false) {
            // Use the DrawPixel function to draw the circle
            // Note: this is extremely inefficient as each pixel is drawn separately
            for (var i = 0; i < 360; i++) {
                var angle = i * System.Math.PI / 180;
                var x1 = (int)(x + radius * System.Math.Cos(angle));
                var y1 = (int)(y + radius * System.Math.Sin(angle));
                DrawPixel(x1, y1, color, thickness, thickness);

                if (filled) {
                    for (var j = 0; j < radius; j++) {
                        var x2 = (int)(x + j * System.Math.Cos(angle));
                        var y2 = (int)(y + j * System.Math.Sin(angle));
                        DrawPixel(x2, y2, color, thickness, thickness);
                    }
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
        /// <params name="filled"> Whether the rectangle should be filled </params>
        public static void DrawRectangle(int x, int y, int width, int height, SFML.Graphics.Color color, int thickness=1, bool filled=false) {
            // Use the DrawPixel function to draw the rectangle
            // Note: this is extremely inefficient as each pixel is drawn separately
            for (var i = 0; i < width; i++) {
                for (var j = 0; j < height; j++) {
                    DrawPixel(x + i, y + j, color, thickness, thickness);
                }
            }

            if (filled) {
                for (var i = 0; i < width; i++) {
                    DrawPixel(x + i, y, color, thickness, thickness);
                    DrawPixel(x + i, y + height, color, thickness, thickness);
                }
                for (var i = 0; i < height; i++) {
                    DrawPixel(x, y + i, color, thickness, thickness);
                    DrawPixel(x + width, y + i, color, thickness, thickness);
                }
            }
        }
    }
}
