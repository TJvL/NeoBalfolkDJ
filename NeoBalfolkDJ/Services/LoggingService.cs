using System;
using System.IO;
using NeoBalfolkDJ.Models;

namespace NeoBalfolkDJ.Services;

public static class LoggingService
{
    private static readonly string LogDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "NeoBalfolkDJ");
    
    private static readonly string LogFilePath = Path.Combine(LogDirectory, "app.log");
    
    private static readonly object LockObject = new();
    
    private const long MaxLogSizeBytes = 5 * 1024 * 1024; // 5 MB max log size

    static LoggingService()
    {
        try
        {
            Directory.CreateDirectory(LogDirectory);
            
            // Rotate log if too large
            if (File.Exists(LogFilePath) && new FileInfo(LogFilePath).Length > MaxLogSizeBytes)
            {
                var backupPath = Path.Combine(LogDirectory, "app.log.old");
                if (File.Exists(backupPath))
                    File.Delete(backupPath);
                File.Move(LogFilePath, backupPath);
            }
        }
        catch
        {
            // Ignore initialization errors
        }
    }

    public static void Log(LoggingLevel level, string message)
    {
        try
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var logLine = $"[{timestamp}] [{level}] {message}";
            
            lock (LockObject)
            {
                File.AppendAllText(LogFilePath, logLine + Environment.NewLine);
            }
        }
        catch
        {
            // Silently fail if logging fails
        }
    }

    public static void Debug(string message) => Log(LoggingLevel.Debug, message);
    public static void Info(string message) => Log(LoggingLevel.Info, message);
    public static void Warning(string message) => Log(LoggingLevel.Warning, message);
    public static void Error(string message) => Log(LoggingLevel.Error, message);
    
    public static void Error(string message, Exception ex)
    {
        Log(LoggingLevel.Error, $"{message}: {ex.GetType().Name} - {ex.Message}");
        if (ex.StackTrace != null)
        {
            Log(LoggingLevel.Error, $"Stack trace: {ex.StackTrace}");
        }
    }
}
