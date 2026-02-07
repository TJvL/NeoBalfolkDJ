using System;
using System.IO;
using System.Threading;
using NeoBalfolkDJ.Models;

namespace NeoBalfolkDJ.Services;

/// <summary>
/// File-based logging service implementation.
/// </summary>
public sealed class LoggingService : ILoggingService, IDisposable
{
    private readonly string _logDirectory;
    private readonly Lock _lockObject = new();
    private const long MaxLogSizeBytes = 256 * 1024; // 256 KB max log size for easy sharing
    private bool _disposed;

    /// <summary>
    /// Gets the path to the current log file.
    /// </summary>
    public string LogFilePath { get; }

    public LoggingService()
    {
        _logDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "NeoBalfolkDJ");
        LogFilePath = Path.Combine(_logDirectory, "app.log");

        Initialize();
    }

    private void Initialize()
    {
        try
        {
            Directory.CreateDirectory(_logDirectory);

            // Clear log if too large (no backup needed, users can export if needed)
            if (File.Exists(LogFilePath) && new FileInfo(LogFilePath).Length > MaxLogSizeBytes)
            {
                File.Delete(LogFilePath);
            }
        }
        catch
        {
            // Ignore initialization errors
        }
    }

    public void Log(LoggingLevel level, string message)
    {
        if (_disposed) return;

        try
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var logLine = $"[{timestamp}] [{level}] {message}";

            lock (_lockObject)
            {
                File.AppendAllText(LogFilePath, logLine + Environment.NewLine);
            }
        }
        catch
        {
            // Silently fail if logging fails
        }
    }

    public void Debug(string message) => Log(LoggingLevel.Debug, message);
    public void Info(string message) => Log(LoggingLevel.Info, message);
    public void Warning(string message) => Log(LoggingLevel.Warning, message);
    public void Error(string message) => Log(LoggingLevel.Error, message);

    public void Error(string message, Exception ex)
    {
        Log(LoggingLevel.Error, $"{message}: {ex.GetType().Name} - {ex.Message}");
        if (ex.StackTrace != null)
        {
            Log(LoggingLevel.Error, $"Stack trace: {ex.StackTrace}");
        }
    }

    public void Critical(string message, Exception ex)
    {
        Log(LoggingLevel.Critical, $"CRITICAL: {message}: {ex.GetType().Name} - {ex.Message}");
        if (ex.StackTrace != null)
        {
            Log(LoggingLevel.Critical, $"Stack trace: {ex.StackTrace}");
        }
        if (ex.InnerException != null)
        {
            Log(LoggingLevel.Critical, $"Inner exception: {ex.InnerException.GetType().Name} - {ex.InnerException.Message}");
            if (ex.InnerException.StackTrace != null)
            {
                Log(LoggingLevel.Critical, $"Inner stack trace: {ex.InnerException.StackTrace}");
            }
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;
    }
}
