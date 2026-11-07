// Re-export SFML.Graphics.Color globally so consumers of PixelDot
// never need to reference SFML directly just to use Color.
global using Color = SFML.Graphics.Color;
