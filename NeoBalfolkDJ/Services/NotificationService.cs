using System;
using NeoBalfolkDJ.Models;

namespace NeoBalfolkDJ.Services;

/// <summary>
/// Service for showing user notifications.
/// </summary>
public sealed class NotificationService(ILoggingService logger) : INotificationService, IDisposable
{
    private readonly ILoggingService _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private bool _disposed;

    public event EventHandler<NotificationEventArgs>? NotificationRequested;

    public void ShowNotification(string message, NotificationSeverity severity)
    {
        if (_disposed) return;

        // Log all notifications automatically
        var logLevel = severity switch
        {
            NotificationSeverity.Information => LoggingLevel.Info,
            NotificationSeverity.Warning => LoggingLevel.Warning,
            NotificationSeverity.Error => LoggingLevel.Error,
            _ => LoggingLevel.Info
        };
        _logger.Log(logLevel, $"[Notification] {message}");

        NotificationRequested?.Invoke(this, new NotificationEventArgs(message, severity));
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        NotificationRequested = null;
    }
}

public class NotificationEventArgs(string message, NotificationSeverity severity) : EventArgs
{
    public string Message { get; } = message;
    public NotificationSeverity Severity { get; } = severity;
}
