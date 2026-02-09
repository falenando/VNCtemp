using System;
using System.IO;

namespace RemoteSupport.HostApp;

internal static class Logger
{
    private static readonly object SyncLock = new();
    private static readonly string LogDirectory = @"C:\ProgramData\RemoteSupport\logs";
    private static readonly string LogFilePath = Path.Combine(LogDirectory, "hostapp.log");

    public static event Action<string>? LogEmitted;

    public static void Info(string message) => Write("INFO", message);
    public static void Warning(string message) => Write("WARN", message);
    public static void Error(string message) => Write("ERROR", message);

    private static void Write(string level, string message)
    {
        var timestamp = DateTimeOffset.Now.ToString("u");
        var line = $"[{timestamp}] [{level}] {message}";

        lock (SyncLock)
        {
            Directory.CreateDirectory(LogDirectory);
            File.AppendAllText(LogFilePath, line + Environment.NewLine);
        }

        LogEmitted?.Invoke(line);
    }
}
