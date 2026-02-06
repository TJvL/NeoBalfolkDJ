using System;
using NeoBalfolkDJ.Models;

namespace NeoBalfolkDJ.Services;

public class NotificationService
{
    public event EventHandler<NotificationEventArgs>? NotificationRequested;

    public void ShowNotification(string message, NotificationSeverity severity)
    {
        // Log all notifications automatically
        var logLevel = severity switch
        {
            NotificationSeverity.Information => LoggingLevel.Info,
            NotificationSeverity.Warning => LoggingLevel.Warning,
            NotificationSeverity.Error => LoggingLevel.Error,
            _ => LoggingLevel.Info
        };
        LoggingService.Log(logLevel, $"[Notification] {message}");
        
        NotificationRequested?.Invoke(this, new NotificationEventArgs(message, severity));
    }
}

public class NotificationEventArgs(string message, NotificationSeverity severity) : EventArgs
{
    public string Message { get; } = message;
    public NotificationSeverity Severity { get; } = severity;
}
