/*
 *   Copyright (c) 2024 Az Foxxo (@AzFoxxo on GitHub)
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

namespace PixelDot;

public static class Logger
{
    public enum Levels
    {
        FATAL,
        ERROR,
        WARN,
        INFO,
        DEBUG,
        TRACE
    }

    private static Levels _defaultLevel;
    private static bool _logToFile;
    private static bool _logTime;
    private static string? _logPath;
    private static string? _logName;
    private static bool _initialized;

    /// <summary> Setup the logger. </summary>
    /// <param name="logToFile"> Log to a file </param>
    /// <param name="logTime"> Include the time in the log </param>
    /// <param name="logPath"> Destination directory for log files </param>
    /// <param name="defaultLevel"> Default log level </param>
    public static void Setup(bool logToFile = false, bool logTime = false, string logPath = "log/", Levels defaultLevel = Levels.DEBUG)
    {
        _logToFile = logToFile;
        _logTime = logTime;
        _logPath = logPath;
        _defaultLevel = defaultLevel;
        _logName = $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss-fff}.log";
        _initialized = true;
    }

    #region Specific Level Log Methods
    public static string LogFatal<T>(T value) => Log(value, Levels.FATAL);
    public static string LogError<T>(T value) => Log(value, Levels.ERROR);
    public static string LogWarn<T>(T value) => Log(value, Levels.WARN);
    public static string LogInfo<T>(T value) => Log(value, Levels.INFO);
    public static string LogDebug<T>(T value) => Log(value, Levels.DEBUG);
    public static string LogTrace<T>(T value) => Log(value, Levels.TRACE);
    public static string Log<T>(T value) => Log(value, _defaultLevel);
    #endregion

    /// <summary> Log a message to the console and optional log file. </summary>
    public static string Log<T>(T value, Levels level = Levels.DEBUG)
    {
        if (!_initialized)
        {
            throw new InvalidOperationException("Logger is not initialized. Please call Logger.Setup() before using the logger.");
        }

        var prefix = level switch
        {
            Levels.FATAL => "[FATAL]",
            Levels.ERROR => "[ERROR]",
            Levels.WARN => "[WARN]",
            Levels.INFO => "[INFO]",
            Levels.DEBUG => "[DEBUG]",
            Levels.TRACE => "[TRACE]",
            _ => "[INFO]"
        };

        var consoleColor = level switch
        {
            Levels.FATAL => ConsoleColor.Red,
            Levels.ERROR => ConsoleColor.DarkRed,
            Levels.WARN => ConsoleColor.Yellow,
            Levels.INFO => ConsoleColor.White,
            Levels.DEBUG => ConsoleColor.DarkGray,
            Levels.TRACE => ConsoleColor.DarkGray,
            _ => ConsoleColor.White
        };

        var timestamp = _logTime ? $"{DateTime.Now:HH:mm:ss.fff} " : "";
        var message = $"{prefix} {timestamp}{value}";

        var prevColor = Console.ForegroundColor;
        Console.ForegroundColor = consoleColor;
        Console.WriteLine(message);
        Console.ForegroundColor = prevColor;

        if (_logToFile && !string.IsNullOrEmpty(_logPath) && !string.IsNullOrEmpty(_logName))
        {
            Directory.CreateDirectory(_logPath);
            var filePath = Path.Combine(_logPath, _logName);
            File.AppendAllText(filePath, message + Environment.NewLine);
        }

        return message;
    }
}
