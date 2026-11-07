using System.Diagnostics;
using SFML.Graphics;
using SFML.System;

namespace PixelDot;

/// <summary>
/// Text rendering library for PixelDot, built on top of SFML with pixel-scale preservation.
/// </summary>
public static class Text
{
    public enum Alignment
    {
        Left,
        Right,
        Centre,
    }

    public const Alignment Left = Alignment.Left;
    public const Alignment Right = Alignment.Right;
    public const Alignment Centre = Alignment.Centre;

    private static Font? _font;
    private static readonly Dictionary<string, string> _fontPathCache = new(StringComparer.OrdinalIgnoreCase);
    private static Shader? _thresholdShader;
    private static bool _shaderInitialised;

    public static int CharacterSize { get; private set; } = 16;
    public static string CurrentText { get; set; } = string.Empty;
    public static Color Color { get; set; } = Color.White;
    public static bool AntiAliasing { get; set; } = false;

    /// <summary>
    /// Enables or disables anti-aliasing for text rendering (default is false for retro pixel art look).
    /// </summary>
    public static void SetAntiAliasing(bool enabled) => AntiAliasing = enabled;

    /// <summary>
    /// Sets the active font from a file path or a system-installed font name (e.g. "Arial", "DejaVu Sans", "LiberationMono").
    /// </summary>
    /// <param name="fontNameOrPath">Font file path or system font family/name</param>
    public static void SetFont(string fontNameOrPath)
    {
        var resolvedPath = ResolveFontPath(fontNameOrPath);
        if (resolvedPath is null)
        {
            Logger.LogError($"Font not found: `{fontNameOrPath}` (checked relative paths, absolute paths, and system fonts)");
            return;
        }

        try
        {
            _font?.Dispose();
            _font = new Font(resolvedPath);
            _font.SetSmooth(false);
            Logger.LogInfo($"Loaded font: {Path.GetFileName(resolvedPath)}");
        }
        catch (Exception ex)
        {
            Logger.LogError($"Failed to load font from '{resolvedPath}': {ex.Message}");
        }
    }

    /// <summary>
    /// Sets the active font size in logical pixels
    /// </summary>
    /// <param name="size">Font size in logical pixels</param>
    public static void SetSize(int size)
    {
        CharacterSize = Math.Max(1, size);
        _font?.SetSmooth(false);
    }

    /// <summary>
    /// Sets the current default text string
    /// </summary>
    /// <param name="text">Text string</param>
    public static void SetText(string text)
    {
        CurrentText = text ?? string.Empty;
    }

    /// <summary>
    /// Sets the default text color
    /// </summary>
    /// <param name="color">Text color</param>
    public static void SetColor(Color color)
    {
        Color = color;
    }

    /// <summary>
    /// Draws the current text at the specified logical coordinates with alignment
    /// </summary>
    public static void Draw(int x, int y, Alignment alignment = Left) =>
        Draw(CurrentText, x, y, alignment, Color);

    /// <summary>
    /// Draws text at the specified logical coordinates with alignment
    /// </summary>
    public static void Draw(int x, int y, string text, Alignment alignment = Left, Color? color = null) =>
        Draw(text, x, y, alignment, color);

    /// <summary>
    /// Draws text at the specified logical coordinates with alignment and color
    /// </summary>
    public static void Draw(int x, int y, string text, Color color, Alignment alignment = Left) =>
        Draw(text, x, y, alignment, color);

    /// <summary>
    /// Draws text at the specified logical coordinates with color and alignment
    /// </summary>
    public static void Draw(string text, int x, int y, Color color, Alignment alignment = Left) =>
        Draw(text, x, y, alignment, color);

    /// <summary>
    /// Main draw routine for text
    /// </summary>
    public static void Draw(string text, int x, int y, Alignment alignment = Left, Color? color = null)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        if (_font is null)
        {
            Logger.LogError("Font is not loaded. Please call Text.SetFont(str) before drawing text.");
            return;
        }

        if (Core.Window is null)
        {
            return;
        }

        // Disable smoothing on the font so glyph scaling maintains crisp integer-block pixels
        _font.SetSmooth(false);

        using var sfmlText = new SFML.Graphics.Text(text, _font, (uint)CharacterSize)
        {
            FillColor = color ?? Color,
            Scale = new Vector2f(Core.PixelScale, Core.PixelScale)
        };

        var bounds = sfmlText.GetLocalBounds();

        // Calculate logical position using bounds and specified alignment
        var logicalX = alignment switch
        {
            Alignment.Right => x - bounds.Left - bounds.Width,
            Alignment.Centre => x - bounds.Left - (bounds.Width / 2f),
            _ => x - bounds.Left
        };

        var logicalY = y - bounds.Top;

        // Multiply by PixelScale and round to exact integer screen coordinates (no subpixels)
        var screenX = (int)MathF.Round(logicalX) * Core.PixelScale;
        var screenY = (int)MathF.Round(logicalY) * Core.PixelScale;
        sfmlText.Position = new Vector2f(screenX, screenY);

        // Flush any queued pixel batches to ensure correct rendering order
        Core.RenderBatch();

        // Render the text using SFML's renderer without anti-aliasing
        var shader = !AntiAliasing ? GetThresholdShader() : null;
        if (shader is not null)
        {
            Core.Window.Draw(sfmlText, new RenderStates(shader));
        }
        else
        {
            Core.Window.Draw(sfmlText);
        }
    }

    /// <summary>
    /// Gets or Initialises a fragment shader that thresholds font alpha to remove FreeType smoothing/anti-aliasing.
    /// </summary>
    private static Shader? GetThresholdShader()
    {
        if (_shaderInitialised)
        {
            return _thresholdShader;
        }

        _shaderInitialised = true;

        if (!Shader.IsAvailable)
        {
            return null;
        }

        try
        {
            const string fragmentShader = @"
                uniform sampler2D texture;
                void main()
                {
                    vec4 pixel = texture2D(texture, gl_TexCoord[0].xy);
                    if (pixel.a < 0.5)
                        discard;
                    gl_FragColor = gl_Color;
                }";

            _thresholdShader = Shader.FromString(null, null, fragmentShader);
            _thresholdShader.SetUniform("texture", Shader.CurrentTexture);
        }
        catch (Exception ex)
        {
            Logger.LogDebug($"Could not Initialise anti-aliasing threshold shader: {ex.Message}");
            _thresholdShader = null;
        }

        return _thresholdShader;
    }

    /// <summary>
    /// Resolves a font path from a direct file path or a system-installed font name.
    /// </summary>
    private static string? ResolveFontPath(string fontNameOrPath)
    {
        if (string.IsNullOrWhiteSpace(fontNameOrPath))
        {
            return null;
        }

        if (_fontPathCache.TryGetValue(fontNameOrPath, out var cached) && File.Exists(cached))
        {
            return cached;
        }

        // 1. Direct file path check (absolute or relative)
        if (File.Exists(fontNameOrPath))
        {
            var fullPath = Path.GetFullPath(fontNameOrPath);
            _fontPathCache[fontNameOrPath] = fullPath;
            return fullPath;
        }

        // Check relative with common font extensions
        if (File.Exists(fontNameOrPath + ".ttf"))
        {
            var fullPath = Path.GetFullPath(fontNameOrPath + ".ttf");
            _fontPathCache[fontNameOrPath] = fullPath;
            return fullPath;
        }

        if (File.Exists(fontNameOrPath + ".otf"))
        {
            var fullPath = Path.GetFullPath(fontNameOrPath + ".otf");
            _fontPathCache[fontNameOrPath] = fullPath;
            return fullPath;
        }

        // 2. Search platform-specific system font directories
        var searchName = Path.GetFileNameWithoutExtension(fontNameOrPath);
        var normalizedSearch = NormalizeFontName(searchName);

        var fontDirectories = GetSystemFontDirectories();
        string? partialMatch = null;

        foreach (var dir in fontDirectories)
        {
            if (!Directory.Exists(dir))
            {
                continue;
            }

            try
            {
                var files = Directory.EnumerateFiles(dir, "*.*", SearchOption.AllDirectories)
                    .Where(f => f.EndsWith(".ttf", StringComparison.OrdinalIgnoreCase) ||
                                f.EndsWith(".otf", StringComparison.OrdinalIgnoreCase) ||
                                f.EndsWith(".ttc", StringComparison.OrdinalIgnoreCase));

                foreach (var file in files)
                {
                    var fileBase = Path.GetFileNameWithoutExtension(file);
                    var normalizedFile = NormalizeFontName(fileBase);

                    // Exact normalized match (e.g. "DejaVuSans" == "dejavusans")
                    if (string.Equals(normalizedFile, normalizedSearch, StringComparison.OrdinalIgnoreCase))
                    {
                        _fontPathCache[fontNameOrPath] = file;
                        return file;
                    }

                    // Secondary match (e.g. "DejaVuSans-Bold" matches "DejaVuSans")
                    if (partialMatch is null && normalizedFile.StartsWith(normalizedSearch, StringComparison.OrdinalIgnoreCase))
                    {
                        partialMatch = file;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogDebug($"Error scanning font directory '{dir}': {ex.Message}");
            }
        }

        if (partialMatch is not null)
        {
            _fontPathCache[fontNameOrPath] = partialMatch;
            return partialMatch;
        }

        // 3. Fallback on Linux using fontconfig (fc-match)
        if (OperatingSystem.IsLinux())
        {
            try
            {
                using var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "fc-match",
                        Arguments = $"-f \"%{{file}}\" \"{fontNameOrPath}\"",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                var output = process.StandardOutput.ReadToEnd().Trim().Trim('"');
                process.WaitForExit(1000);

                if (!string.IsNullOrEmpty(output) && File.Exists(output))
                {
                    _fontPathCache[fontNameOrPath] = output;
                    return output;
                }
            }
            catch
            {
                // fc-match not installed or not available
            }
        }

        return null;
    }

    private static string NormalizeFontName(string name) =>
        name.Replace(" ", "", StringComparison.Ordinal)
            .Replace("-", "", StringComparison.Ordinal)
            .Replace("_", "", StringComparison.Ordinal);

    private static IEnumerable<string> GetSystemFontDirectories()
    {
        if (OperatingSystem.IsWindows())
        {
            var winFonts = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);
            if (!string.IsNullOrEmpty(winFonts))
            {
                yield return winFonts;
            }

            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (!string.IsNullOrEmpty(localAppData))
            {
                yield return Path.Combine(localAppData, "Microsoft", "Windows", "Fonts");
            }
        }
        else if (OperatingSystem.IsMacOS())
        {
            yield return "/System/Library/Fonts";
            yield return "/Library/Fonts";
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (!string.IsNullOrEmpty(home))
            {
                yield return Path.Combine(home, "Library", "Fonts");
            }
        }
        else if (OperatingSystem.IsLinux())
        {
            yield return "/usr/share/fonts";
            yield return "/usr/local/share/fonts";
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (!string.IsNullOrEmpty(home))
            {
                yield return Path.Combine(home, ".fonts");
                yield return Path.Combine(home, ".local", "share", "fonts");
            }
        }
    }
}
