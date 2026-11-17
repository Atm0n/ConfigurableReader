using System;
using System.IO;

namespace ConfigurableReader.Common;

/// <summary>
/// Minimal file logger. Writes to %APPDATA%\ConfigurableReader\logs\app.log (rotated at 1 MB)
/// and mirrors every entry to the debug output. Logging never throws.
/// </summary>
public static class AppLog
{
    private const long MaxLogSizeBytes = 1024 * 1024;
    private static readonly object Lock = new();

    public static string LogDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ConfigurableReader",
        "logs");

    public static string LogFilePath { get; } = Path.Combine(LogDirectory, "app.log");

    public static void Info(string message) => Write("INFO", message, null);

    public static void Error(string message, Exception? ex = null) => Write("ERROR", message, ex);

    private static void Write(string level, string message, Exception? ex)
    {
        string line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {message}";
        if (ex != null)
        {
            line += Environment.NewLine + ex;
        }

        System.Diagnostics.Debug.WriteLine(line);

        try
        {
            lock (Lock)
            {
                Directory.CreateDirectory(LogDirectory);
                RotateIfNeeded();
                File.AppendAllText(LogFilePath, line + Environment.NewLine);
            }
        }
        catch
        {
            // Logging must never take the app down.
        }
    }

    private static void RotateIfNeeded()
    {
        var info = new FileInfo(LogFilePath);
        if (info.Exists && info.Length > MaxLogSizeBytes)
        {
            File.Move(LogFilePath, LogFilePath + ".1", overwrite: true);
        }
    }
}
