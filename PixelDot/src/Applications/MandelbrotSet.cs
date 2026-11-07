
using PixelDot;

namespace Applications;

[RunnableApplication("Mandelbrot Set", "1.0.0", "Az Foxxo", "Mandelbrot fractal")]
public class MandelbrotSet : Application
{
    private const int MaxIterations = 50;

    // View state
    private double centreX = -0.75;
    private double centreY = 0.0;
    private double zoom = 1.0;

    public override void OnUpdate()
    {
        double moveSpeedX = 0.05 / zoom;
        double moveSpeedY = 0.05 / zoom;
        double zoomFactor = 1.05;

        // Move left, right, up and down
        if (Input.IsHeld(Keys.Left)) centreX -= moveSpeedX;
        if (Input.IsHeld(Keys.Right)) centreX += moveSpeedX;
        if (Input.IsHeld(Keys.Up)) centreY -= moveSpeedY;
        if (Input.IsHeld(Keys.Down)) centreY += moveSpeedY;

        // Zoom in/out
        if (Input.IsHeld(MouseButtons.Left)) zoom *= zoomFactor;
        if (Input.IsHeld(MouseButtons.Right)) zoom /= zoomFactor;

        double scaleX = 3.5 / zoom;
        double scaleY = 2.0 / zoom;

        for (int x = 0; x < Core.ScreenWidth; x++)
        {
            // Convert pixel position into a complex number
            double a = (x - Core.ScreenWidth / 2.0) * scaleX / Core.ScreenWidth + centreX;

            for (int y = 0; y < Core.ScreenHeight; y++)
            {
                double b = (y - Core.ScreenHeight / 2.0) * scaleY / Core.ScreenHeight + centreY;

                double zx = 0;
                double zy = 0;

                int iterations = 0;

                // Mandelbrot iteration
                while (zx * zx + zy * zy < 4 && iterations < MaxIterations)
                {
                    double temp = zx * zx - zy * zy + a;
                    zy = 2 * zx * zy + b;
                    zx = temp;
                    iterations++;
                }

                // Colour based on how quickly the point escaped
                byte colourIndex = (byte)(iterations % Palette.Colours.Length);
                var colour = Palette.GetColour(colourIndex);

                Core.DrawPixel(x, y, colour);
            }
        }
    }
}
