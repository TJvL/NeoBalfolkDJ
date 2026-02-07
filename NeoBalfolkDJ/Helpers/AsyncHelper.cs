using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NeoBalfolkDJ.Models;
using NeoBalfolkDJ.Services;

namespace NeoBalfolkDJ.Helpers;

/// <summary>
/// Helper for safely executing async operations in event handlers.
/// 
/// <para>
/// <b>Purpose:</b> In C#, event handlers must be <c>async void</c>, but unhandled exceptions
/// in <c>async void</c> methods crash the entire application. This helper wraps async operations
/// in try-catch blocks to prevent crashes and provide consistent error handling.
/// </para>
/// 
/// <para>
/// <b>Usage:</b> Instead of writing <c>async void OnSomeEvent(...)</c>, use:
/// <code>
/// private void OnSomeEvent(object? sender, EventArgs e)
/// {
///     AsyncHelper.SafeFireAndForget(async () =>
///     {
///         // Your async code here
///     }, userFriendlyError: "Failed to perform operation");
/// }
/// </code>
/// </para>
/// 
/// <para>
/// <b>Error Handling:</b>
/// <list type="bullet">
///   <item>All exceptions are logged via <see cref="ILoggingService"/></item>
///   <item>User notifications show a user-friendly message if provided</item>
///   <item>If no user-friendly message is provided, shows generic "An unexpected error occurred"</item>
///   <item><see cref="OperationCanceledException"/> is silently ignored (expected for cancellation)</item>
/// </list>
/// </para>
/// </summary>
public static class AsyncHelper
{
    /// <summary>
    /// Executes an async operation safely, catching and logging any exceptions.
    /// Use this when you need to call async code from an event handler (async void context).
    /// </summary>
    /// <param name="operation">The async operation to execute.</param>
    /// <param name="userFriendlyError">
    /// User-friendly error message to show in notification. 
    /// Use this for user-initiated operations (file I/O, imports, exports) where the user should know what failed.
    /// Pass <c>null</c> for internal operations where a generic "Unexpected error" message is appropriate.
    /// </param>
    /// <param name="showNotification">Whether to show a notification to the user on failure. Defaults to true.</param>
    public static async void SafeFireAndForget(
        Func<Task> operation,
        string? userFriendlyError = null,
        bool showNotification = true)
    {
        try
        {
            await operation();
        }
        catch (OperationCanceledException)
        {
            // Task was canceled - this is expected behavior, don't log as error
        }
        catch (Exception ex)
        {
            HandleException(ex, userFriendlyError, showNotification);
        }
    }

    /// <summary>
    /// Executes an async operation safely with a return value.
    /// </summary>
    /// <typeparam name="T">The return type of the operation.</typeparam>
    /// <param name="operation">The async operation to execute.</param>
    /// <param name="defaultValue">The default value to return on failure.</param>
    /// <param name="userFriendlyError">User-friendly error message for notifications.</param>
    /// <param name="showNotification">Whether to show a notification to the user on failure.</param>
    /// <returns>The result of the operation, or the default value on failure.</returns>
    public static async Task<T?> SafeExecuteAsync<T>(
        Func<Task<T>> operation,
        T? defaultValue = default,
        string? userFriendlyError = null,
        bool showNotification = true)
    {
        try
        {
            return await operation();
        }
        catch (OperationCanceledException)
        {
            return defaultValue;
        }
        catch (Exception ex)
        {
            HandleException(ex, userFriendlyError, showNotification);
            return defaultValue;
        }
    }

    private static void HandleException(Exception ex, string? userFriendlyError, bool showNotification)
    {
        // Always log the full exception details
        Program.Logger.Error("Unhandled exception in async operation", ex);

        if (showNotification)
        {
            var notificationService = App.Services.GetService<INotificationService>();
            if (notificationService != null)
            {
                // Show user-friendly message if provided, otherwise generic message
                var displayMessage = userFriendlyError ?? "An unexpected error occurred";
                notificationService.ShowNotification(displayMessage, NotificationSeverity.Error);
            }
        }
    }
}

