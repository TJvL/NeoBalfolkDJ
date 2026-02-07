using System;
using NeoBalfolkDJ.Models;

namespace NeoBalfolkDJ.Services;

/// <summary>
/// Service for showing user notifications.
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Event raised when a notification should be displayed.
    /// </summary>
    event EventHandler<NotificationEventArgs>? NotificationRequested;

    /// <summary>
    /// Shows a notification to the user.
    /// </summary>
    void ShowNotification(string message, NotificationSeverity severity);
}

