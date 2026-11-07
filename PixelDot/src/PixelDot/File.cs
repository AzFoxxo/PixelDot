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
using System.Text;

namespace PixelDot;

/// <summary>
/// Provides static methods for file and directory manipulation, binary and text I/O,
/// and cross-platform native file selection dialogues.
/// </summary>
public static class File
{
    #region File Dialogues

    /// <summary>
    /// Displays a native file open dialogue and returns the selected file path, or null if cancelled.
    /// </summary>
    /// <param name="title">Title of the dialogue window</param>
    /// <param name="defaultPath">Initial directory or file path</param>
    /// <param name="filter">Filter string (e.g. "Text Files (*.txt)|*.txt|All Files (*.*)|*.*" or "*.txt")</param>
    /// <returns>Selected file path, or null if cancelled or failed</returns>
    public static string? OpenFileDialogue(string? title = "Open File", string? defaultPath = null, string? filter = null)
    {
        return ShowNativeFileDialogue(saveMode: false, selectFolder: false, title, defaultPath, filter, null);
    }

    /// <summary>
    /// Alias for <see cref="OpenFileDialogue"/>.
    /// </summary>
    public static string? OpenFileDialog(string? title = "Open File", string? defaultPath = null, string? filter = null) =>
        OpenFileDialogue(title, defaultPath, filter);

    /// <summary>
    /// Displays a native file save dialogue and returns the chosen file path, or null if cancelled.
    /// </summary>
    /// <param name="title">Title of the dialogue window</param>
    /// <param name="defaultPath">Initial directory or file path</param>
    /// <param name="filter">Filter string (e.g. "Text Files (*.txt)|*.txt|All Files (*.*)|*.*")</param>
    /// <param name="defaultExtension">Optional default extension (e.g. ".txt" or "txt") if not provided in the filename</param>
    /// <returns>Chosen file path, or null if cancelled or failed</returns>
    public static string? SaveFileDialogue(string? title = "Save File", string? defaultPath = null, string? filter = null, string? defaultExtension = null)
    {
        var result = ShowNativeFileDialogue(saveMode: true, selectFolder: false, title, defaultPath, filter, defaultExtension);
        if (result is not null && !string.IsNullOrWhiteSpace(defaultExtension))
        {
            var ext = defaultExtension.StartsWith('.') ? defaultExtension : "." + defaultExtension;
            if (!Path.HasExtension(result))
            {
                result += ext;
            }
        }
        return result;
    }

    /// <summary>
    /// Alias for <see cref="SaveFileDialogue"/>.
    /// </summary>
    public static string? SaveFileDialog(string? title = "Save File", string? defaultPath = null, string? filter = null, string? defaultExtension = null) =>
        SaveFileDialogue(title, defaultPath, filter, defaultExtension);

    /// <summary>
    /// Displays a native folder picker dialogue and returns the chosen directory path, or null if cancelled.
    /// </summary>
    /// <param name="title">Title of the dialogue window</param>
    /// <param name="defaultPath">Initial directory path</param>
    /// <returns>Selected folder path, or null if cancelled or failed</returns>
    public static string? SelectFolderDialogue(string? title = "Select Folder", string? defaultPath = null)
    {
        return ShowNativeFileDialogue(saveMode: false, selectFolder: true, title, defaultPath, null, null);
    }

    /// <summary>
    /// Alias for <see cref="SelectFolderDialogue"/>.
    /// </summary>
    public static string? SelectFolderDialog(string? title = "Select Folder", string? defaultPath = null) =>
        SelectFolderDialogue(title, defaultPath);

    #endregion

    #region File & Directory Management

    /// <summary>
    /// Opens a <see cref="FileStream"/> on the specified path with the specified mode and access.
    /// </summary>
    public static FileStream Open(string path, FileMode mode, FileAccess access = FileAccess.ReadWrite, FileShare share = FileShare.None) =>
        System.IO.File.Open(path, mode, access, share);

    /// <summary>
    /// Opens an existing file for reading.
    /// </summary>
    public static FileStream OpenRead(string path) =>
        System.IO.File.OpenRead(path);

    /// <summary>
    /// Opens an existing file or creates a new file for writing.
    /// </summary>
    public static FileStream OpenWrite(string path) =>
        System.IO.File.OpenWrite(path);

    /// <summary>
    /// Creates or overwrites a file at the specified path.
    /// </summary>
    public static FileStream Create(string path) =>
        System.IO.File.Create(path);

    /// <summary>
    /// Creates or overwrites a file at the specified path with a custom buffer size.
    /// </summary>
    public static FileStream Create(string path, int bufferSize) =>
        System.IO.File.Create(path, bufferSize);

    /// <summary>
    /// Creates all directories and subdirectories in the specified path unless they already exist.
    /// </summary>
    public static DirectoryInfo CreateDirectory(string path) =>
        Directory.CreateDirectory(path);

    /// <summary>
    /// Deletes the specified file.
    /// </summary>
    public static void Delete(string path) =>
        System.IO.File.Delete(path);

    /// <summary>
    /// Deletes the specified directory and, if specified, all subdirectories and files.
    /// </summary>
    public static void DeleteDirectory(string path, bool recursive = true) =>
        Directory.Delete(path, recursive);

    /// <summary>
    /// Determines whether the specified file exists.
    /// </summary>
    public static bool Exists(string? path) =>
        System.IO.File.Exists(path);

    /// <summary>
    /// Determines whether the specified directory exists.
    /// </summary>
    public static bool DirectoryExists(string? path) =>
        Directory.Exists(path);

    /// <summary>
    /// Copies an existing file to a new file.
    /// </summary>
    public static void Copy(string sourceFileName, string destFileName, bool overwrite = false) =>
        System.IO.File.Copy(sourceFileName, destFileName, overwrite);

    /// <summary>
    /// Moves a file to a new location, optionally overwriting an existing file.
    /// </summary>
    public static void Move(string sourceFileName, string destFileName, bool overwrite = false) =>
        System.IO.File.Move(sourceFileName, destFileName, overwrite);

    /// <summary>
    /// Returns the names of files (including their paths) in the specified directory.
    /// </summary>
    public static string[] GetFiles(string path, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly) =>
        Directory.GetFiles(path, searchPattern, searchOption);

    /// <summary>
    /// Returns the names of subdirectories (including their paths) in the specified directory.
    /// </summary>
    public static string[] GetDirectories(string path, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly) =>
        Directory.GetDirectories(path, searchPattern, searchOption);

    #endregion

    #region Text I/O

    /// <summary>
    /// Opens a text file, reads all contents of the file, and closes the file.
    /// </summary>
    public static string ReadAllText(string path, Encoding? encoding = null) =>
        encoding is null ? System.IO.File.ReadAllText(path) : System.IO.File.ReadAllText(path, encoding);

    /// <summary>
    /// Creates a new file, writes the specified string to the file, and closes the file.
    /// </summary>
    public static void WriteAllText(string path, string? contents, Encoding? encoding = null)
    {
        if (encoding is null)
            System.IO.File.WriteAllText(path, contents);
        else
            System.IO.File.WriteAllText(path, contents, encoding);
    }

    /// <summary>
    /// Opens a text file, reads all lines of the file, and closes the file.
    /// </summary>
    public static string[] ReadAllLines(string path, Encoding? encoding = null) =>
        encoding is null ? System.IO.File.ReadAllLines(path) : System.IO.File.ReadAllLines(path, encoding);

    /// <summary>
    /// Creates a new file, writes a collection of strings to the file, and closes the file.
    /// </summary>
    public static void WriteAllLines(string path, IEnumerable<string> contents, Encoding? encoding = null)
    {
        if (encoding is null)
            System.IO.File.WriteAllLines(path, contents);
        else
            System.IO.File.WriteAllLines(path, contents, encoding);
    }

    /// <summary>
    /// Opens a file, appends the specified string to the file, and closes the file.
    /// </summary>
    public static void AppendAllText(string path, string? contents, Encoding? encoding = null)
    {
        if (encoding is null)
            System.IO.File.AppendAllText(path, contents);
        else
            System.IO.File.AppendAllText(path, contents, encoding);
    }

    /// <summary>
    /// Appends lines to a file, and then closes the file.
    /// </summary>
    public static void AppendAllLines(string path, IEnumerable<string> contents, Encoding? encoding = null)
    {
        if (encoding is null)
            System.IO.File.AppendAllLines(path, contents);
        else
            System.IO.File.AppendAllLines(path, contents, encoding);
    }

    /// <summary>
    /// Opens an existing UTF-8 encoded text file for reading.
    /// </summary>
    public static StreamReader OpenText(string path) =>
        System.IO.File.OpenText(path);

    /// <summary>
    /// Creates or opens a file for writing UTF-8 encoded text.
    /// </summary>
    public static StreamWriter CreateText(string path) =>
        System.IO.File.CreateText(path);

    /// <summary>
    /// Creates a <see cref="StreamWriter"/> that appends UTF-8 encoded text to an existing file.
    /// </summary>
    public static StreamWriter AppendText(string path) =>
        System.IO.File.AppendText(path);

    #endregion

    #region Binary I/O

    /// <summary>
    /// Opens a binary file, reads the contents of the file into a byte array, and then closes the file.
    /// </summary>
    public static byte[] ReadAllBytes(string path) =>
        System.IO.File.ReadAllBytes(path);

    /// <summary>
    /// Creates a new file, writes the specified byte array to the file, and then closes the file.
    /// </summary>
    public static void WriteAllBytes(string path, byte[] bytes) =>
        System.IO.File.WriteAllBytes(path, bytes);

    /// <summary>
    /// Reads the entire contents of a binary file into a byte array. Alias for <see cref="ReadAllBytes"/>.
    /// </summary>
    public static byte[] ReadBinary(string path) =>
        ReadAllBytes(path);

    /// <summary>
    /// Writes a byte array to a binary file. Alias for <see cref="WriteAllBytes"/>.
    /// </summary>
    public static void WriteBinary(string path, byte[] bytes) =>
        WriteAllBytes(path, bytes);

    #endregion

    #region Internal Native Dialog Implementation

    private static string? ShowNativeFileDialogue(bool saveMode, bool selectFolder, string? title, string? defaultPath, string? filter, string? defaultExtension)
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
                return ShowWindowsDialog(saveMode, selectFolder, title, defaultPath, filter, defaultExtension);
            }
            if (OperatingSystem.IsMacOS())
            {
                return ShowMacDialog(saveMode, selectFolder, title, defaultPath, filter);
            }
            if (OperatingSystem.IsLinux())
            {
                return ShowLinuxDialog(saveMode, selectFolder, title, defaultPath, filter);
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarn($"Native file dialogue failed: {ex.Message}");
        }

        // Fallback: try Python Tkinter if available
        return ShowTkinterDialogFallback(saveMode, selectFolder, title, defaultPath, filter);
    }

    private static string? ShowLinuxDialog(bool saveMode, bool selectFolder, string? title, string? defaultPath, string? filter)
    {
        // 1. Try Zenity
        if (CommandExists("zenity"))
        {
            var args = new StringBuilder();
            args.Append("--file-selection");

            if (!string.IsNullOrEmpty(title))
            {
                args.Append($" --title=\"{EscapeArg(title)}\"");
            }

            if (selectFolder)
            {
                args.Append(" --directory");
            }
            else if (saveMode)
            {
                args.Append(" --save --confirm-overwrite");
            }

            if (!string.IsNullOrEmpty(defaultPath))
            {
                args.Append($" --filename=\"{EscapeArg(Path.GetFullPath(defaultPath))}\"");
            }

            if (!selectFolder && !string.IsNullOrWhiteSpace(filter))
            {
                var filters = ParseFilters(filter);
                foreach (var (name, pattern) in filters)
                {
                    args.Append($" --file-filter=\"{EscapeArg(name)} | {EscapeArg(pattern)}\"");
                }
            }

            var output = RunProcess("zenity", args.ToString());
            if (!string.IsNullOrWhiteSpace(output))
            {
                return output.Trim();
            }
        }

        // 2. Try Kdialog
        if (CommandExists("kdialog"))
        {
            var args = new StringBuilder();
            if (!string.IsNullOrEmpty(title))
            {
                args.Append($"--title \"{EscapeArg(title)}\" ");
            }

            if (selectFolder)
            {
                args.Append("--getexistingdirectory");
                if (!string.IsNullOrEmpty(defaultPath))
                {
                    args.Append($" \"{EscapeArg(Path.GetFullPath(defaultPath))}\"");
                }
            }
            else if (saveMode)
            {
                args.Append("--getsavefilename");
                args.Append(string.IsNullOrEmpty(defaultPath) ? " ." : $" \"{EscapeArg(Path.GetFullPath(defaultPath))}\"");
                if (!string.IsNullOrEmpty(filter))
                {
                    args.Append($" \"{EscapeArg(FormatKdialogFilter(filter))}\"");
                }
            }
            else
            {
                args.Append("--getopenfilename");
                args.Append(string.IsNullOrEmpty(defaultPath) ? " ." : $" \"{EscapeArg(Path.GetFullPath(defaultPath))}\"");
                if (!string.IsNullOrEmpty(filter))
                {
                    args.Append($" \"{EscapeArg(FormatKdialogFilter(filter))}\"");
                }
            }

            var output = RunProcess("kdialog", args.ToString());
            if (!string.IsNullOrWhiteSpace(output))
            {
                return output.Trim();
            }
        }

        // 3. Fallback to Python Tkinter
        return ShowTkinterDialogFallback(saveMode, selectFolder, title, defaultPath, filter);
    }

    private static string? ShowWindowsDialog(bool saveMode, bool selectFolder, string? title, string? defaultPath, string? filter, string? defaultExtension)
    {
        var psScript = new StringBuilder();
        psScript.Append("Add-Type -AssemblyName System.Windows.Forms; ");

        if (selectFolder)
        {
            psScript.Append("$d = New-Object System.Windows.Forms.FolderBrowserDialog; ");
            if (!string.IsNullOrEmpty(title))
            {
                psScript.Append($"$d.Description = '{EscapePowerShell(title)}'; ");
            }
            if (!string.IsNullOrEmpty(defaultPath))
            {
                psScript.Append($"$d.SelectedPath = '{EscapePowerShell(Path.GetFullPath(defaultPath))}'; ");
            }
            psScript.Append("if ($d.ShowDialog() -eq [System.Windows.Forms.DialogResult]::OK) { Write-Output $d.SelectedPath }");
        }
        else
        {
            var dialogType = saveMode ? "SaveFileDialog" : "OpenFileDialog";
            psScript.Append($"$d = New-Object System.Windows.Forms.{dialogType}; ");

            if (!string.IsNullOrEmpty(title))
            {
                psScript.Append($"$d.Title = '{EscapePowerShell(title)}'; ");
            }
            if (!string.IsNullOrEmpty(defaultPath))
            {
                var full = Path.GetFullPath(defaultPath);
                if (Directory.Exists(full))
                {
                    psScript.Append($"$d.InitialDirectory = '{EscapePowerShell(full)}'; ");
                }
                else
                {
                    var dir = Path.GetDirectoryName(full);
                    if (!string.IsNullOrEmpty(dir))
                    {
                        psScript.Append($"$d.InitialDirectory = '{EscapePowerShell(dir)}'; ");
                    }
                    var file = Path.GetFileName(full);
                    if (!string.IsNullOrEmpty(file))
                    {
                        psScript.Append($"$d.FileName = '{EscapePowerShell(file)}'; ");
                    }
                }
            }
            if (!string.IsNullOrEmpty(filter))
            {
                psScript.Append($"$d.Filter = '{EscapePowerShell(filter)}'; ");
            }
            if (!string.IsNullOrEmpty(defaultExtension))
            {
                psScript.Append($"$d.DefaultExt = '{EscapePowerShell(defaultExtension.TrimStart('.'))}'; ");
            }

            psScript.Append("if ($d.ShowDialog() -eq [System.Windows.Forms.DialogResult]::OK) { Write-Output $d.FileName }");
        }

        var output = RunProcess("powershell", $"-NoProfile -STA -Command \"{psScript}\"");
        return string.IsNullOrWhiteSpace(output) ? null : output.Trim();
    }

    private static string? ShowMacDialog(bool saveMode, bool selectFolder, string? title, string? defaultPath, string? filter)
    {
        var script = new StringBuilder();
        var prompt = string.IsNullOrEmpty(title) ? "" : $" with prompt \"{EscapeAppleScript(title)}\"";

        if (selectFolder)
        {
            script.Append($"POSIX path of (choose folder{prompt})");
        }
        else if (saveMode)
        {
            var defName = string.IsNullOrEmpty(defaultPath) ? "" : $" default name \"{EscapeAppleScript(Path.GetFileName(defaultPath))}\"";
            script.Append($"POSIX path of (choose file name{prompt}{defName})");
        }
        else
        {
            script.Append($"POSIX path of (choose file{prompt})");
        }

        var output = RunProcess("osascript", $"-e '{script}'");
        return string.IsNullOrWhiteSpace(output) ? null : output.Trim();
    }

    private static string? ShowTkinterDialogFallback(bool saveMode, bool selectFolder, string? title, string? defaultPath, string? filter)
    {
        if (!CommandExists("python3") && !CommandExists("python"))
        {
            return null;
        }

        var py = CommandExists("python3") ? "python3" : "python";
        var pyScript = new StringBuilder();
        pyScript.Append("import tkinter as tk, tkinter.filedialog as fd; ");
        pyScript.Append("root = tk.Tk(); root.withdraw(); root.attributes('-topmost', True); ");

        var t = string.IsNullOrEmpty(title) ? "Select" : title;
        var initialDir = "";
        var initialFile = "";
        if (!string.IsNullOrEmpty(defaultPath))
        {
            var full = Path.GetFullPath(defaultPath);
            if (Directory.Exists(full))
            {
                initialDir = full;
            }
            else
            {
                initialDir = Path.GetDirectoryName(full) ?? "";
                initialFile = Path.GetFileName(full);
            }
        }

        if (selectFolder)
        {
            pyScript.Append($"res = fd.askdirectory(title='{EscapePython(t)}', initialdir='{EscapePython(initialDir)}'); ");
        }
        else if (saveMode)
        {
            pyScript.Append($"res = fd.asksaveasfilename(title='{EscapePython(t)}', initialdir='{EscapePython(initialDir)}', initialfile='{EscapePython(initialFile)}'); ");
        }
        else
        {
            pyScript.Append($"res = fd.askopenfilename(title='{EscapePython(t)}', initialdir='{EscapePython(initialDir)}', initialfile='{EscapePython(initialFile)}'); ");
        }

        pyScript.Append("print(res or '', end='')");

        var output = RunProcess(py, $"-c \"{pyScript}\"");
        return string.IsNullOrWhiteSpace(output) ? null : output.Trim();
    }

    private static List<(string Name, string Pattern)> ParseFilters(string filter)
    {
        var result = new List<(string, string)>();
        if (string.IsNullOrWhiteSpace(filter))
        {
            return result;
        }

        if (!filter.Contains('|'))
        {
            result.Add((filter, filter));
            return result;
        }

        var parts = filter.Split('|');
        for (var i = 0; i + 1 < parts.Length; i += 2)
        {
            var name = parts[i].Trim();
            var pattern = parts[i + 1].Trim().Replace(';', ' ');
            result.Add((name, pattern));
        }

        return result;
    }

    private static string FormatKdialogFilter(string filter)
    {
        if (string.IsNullOrWhiteSpace(filter))
        {
            return "*";
        }

        var filters = ParseFilters(filter);
        var sb = new StringBuilder();
        foreach (var (name, pattern) in filters)
        {
            if (sb.Length > 0) sb.Append('\n');
            sb.Append($"{pattern} | {name}");
        }
        return sb.ToString();
    }

    private static bool CommandExists(string command)
    {
        if (OperatingSystem.IsWindows())
        {
            return true;
        }

        try
        {
            using var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "which",
                    Arguments = command,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            proc.Start();
            proc.WaitForExit(1000);
            return proc.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private static string? RunProcess(string filename, string arguments)
    {
        try
        {
            using var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = filename,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            proc.Start();
            var output = proc.StandardOutput.ReadToEnd();
            proc.WaitForExit();

            if (proc.ExitCode == 0)
            {
                return output;
            }
        }
        catch (Exception ex)
        {
            Logger.LogDebug($"Process '{filename}' execution failed: {ex.Message}");
        }

        return null;
    }

    private static string EscapeArg(string arg) => arg.Replace("\"", "\\\"");
    private static string EscapePowerShell(string arg) => arg.Replace("'", "''");
    private static string EscapeAppleScript(string arg) => arg.Replace("\"", "\\\"");
    private static string EscapePython(string arg) => arg.Replace("\\", "\\\\").Replace("'", "\\'");

    #endregion
}
