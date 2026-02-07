using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NeoBalfolkDJ.Models;
using NeoBalfolkDJ.Services;

namespace NeoBalfolkDJ.Messaging;

/// <summary>
/// Command bus implementation with exception handling, logging, and notifications.
/// </summary>
public sealed class CommandBus(ILoggingService logger, INotificationService notificationService)
    : ICommandBus, IDisposable
{
    private readonly ILoggingService _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly INotificationService _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
    private readonly Dictionary<Type, object> _handlers = new();
    private readonly Lock _lock = new();
    private bool _disposed;

    public async Task SendAsync<TCommand>(TCommand command) where TCommand : class
    {
        if (command == null) throw new ArgumentNullException(nameof(command));
        if (_disposed) throw new ObjectDisposedException(nameof(CommandBus));

        Func<TCommand, Task>? handler;
        lock (_lock)
        {
            if (!_handlers.TryGetValue(typeof(TCommand), out var handlerObj))
            {
                _logger.Warning($"No handler registered for command {typeof(TCommand).Name}");
                return;
            }
            handler = handlerObj as Func<TCommand, Task>;
        }

        if (handler == null)
        {
            _logger.Warning($"Handler for command {typeof(TCommand).Name} has invalid type");
            return;
        }

        try
        {
            await handler(command);
        }
        catch (Exception ex)
        {
            _logger.Error($"Command {typeof(TCommand).Name} failed", ex);
            _notificationService.ShowNotification($"Operation failed: {ex.Message}", NotificationSeverity.Error);
        }
    }

    public IDisposable RegisterHandler<TCommand>(Func<TCommand, Task> handler) where TCommand : class
    {
        if (handler == null) throw new ArgumentNullException(nameof(handler));
        if (_disposed) throw new ObjectDisposedException(nameof(CommandBus));

        lock (_lock)
        {
            if (_handlers.ContainsKey(typeof(TCommand)))
            {
                throw new InvalidOperationException($"Handler already registered for command {typeof(TCommand).Name}");
            }
            _handlers[typeof(TCommand)] = handler;
        }

        _logger.Debug($"Registered handler for command {typeof(TCommand).Name}");
        return new HandlerToken(UnregisterHandler<TCommand>);
    }

    private void UnregisterHandler<TCommand>() where TCommand : class
    {
        lock (_lock)
        {
            _handlers.Remove(typeof(TCommand));
        }
        _logger.Debug($"Unregistered handler for command {typeof(TCommand).Name}");
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        lock (_lock)
        {
            _handlers.Clear();
        }
    }


    private sealed class HandlerToken(Action unregister) : IDisposable
    {
        private Action? _unregister = unregister;
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed) return;
            _unregister?.Invoke();
            _unregister = null;
            _disposed = true;
        }
    }
}

