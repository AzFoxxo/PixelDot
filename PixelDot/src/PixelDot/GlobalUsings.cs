// Re-export SFML.Graphics.Color globally so consumers of PixelDot
// never need to reference SFML directly just to use Color.
global using Color = SFML.Graphics.Color;

// Re-export PixelDot.File globally so consumers of PixelDot
// have direct access to PixelDot's File class and helpers.
global using File = PixelDot.File;

// Re-export PixelDot.Collision globally so consumers of PixelDot
// have direct access to collision classes and components.
global using PixelDot.Collision;
