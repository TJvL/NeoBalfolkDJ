using System;
using NeoBalfolkDJ.Models;

namespace NeoBalfolkDJ.Services;

/// <summary>
/// Service for application logging.
/// </summary>
public interface ILoggingService
{
    /// <summary>
    /// Gets the path to the current log file.
    /// </summary>
    string LogFilePath { get; }
    
    void Log(LoggingLevel level, string message);
    void Debug(string message);
    void Info(string message);
    void Warning(string message);
    void Error(string message);
    void Error(string message, Exception ex);
    
    /// <summary>
    /// Logs a critical error that indicates the application cannot continue.
    /// Used for unhandled exceptions in global handlers.
    /// </summary>
    void Critical(string message, Exception ex);
}
