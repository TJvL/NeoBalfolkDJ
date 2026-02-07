using System;
using System.Collections.Generic;
using System.Threading;

namespace NeoBalfolkDJ.Messaging;

/// <summary>
/// Event aggregator implementation with UI thread marshaling.
/// </summary>
public sealed class EventAggregator(IDispatcher dispatcher) : IEventAggregator, IDisposable
{
    private readonly IDispatcher _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
    private readonly Dictionary<Type, List<object>> _subscribers = new();
    private readonly Lock _lock = new();
    private bool _disposed;

    public void Publish<TEvent>(TEvent evt) where TEvent : class
    {
        if (evt == null) throw new ArgumentNullException(nameof(evt));
        if (_disposed) return;

        List<object> handlers;
        lock (_lock)
        {
            if (!_subscribers.TryGetValue(typeof(TEvent), out var subscriberList))
                return;

            // Copy to avoid modification during enumeration
            handlers = new List<object>(subscriberList);
        }

        // Marshal to UI thread
        _dispatcher.Post(() =>
        {
            foreach (var handler in handlers)
            {
                try
                {
                    ((Action<TEvent>)handler)(evt);
                }
                catch (Exception)
                {
                    // Swallow exceptions from individual handlers to not break other subscribers
                    // Logging would require ILoggingService dependency - handlers should handle their own exceptions
                }
            }
        });
    }

    public IDisposable Subscribe<TEvent>(Action<TEvent> handler) where TEvent : class
    {
        if (handler == null) throw new ArgumentNullException(nameof(handler));
        if (_disposed) throw new ObjectDisposedException(nameof(EventAggregator));

        lock (_lock)
        {
            if (!_subscribers.TryGetValue(typeof(TEvent), out var subscriberList))
            {
                subscriberList = [];
                _subscribers[typeof(TEvent)] = subscriberList;
            }
            subscriberList.Add(handler);
        }

        return new SubscriptionToken(() => Unsubscribe(handler));
    }

    private void Unsubscribe<TEvent>(Action<TEvent> handler) where TEvent : class
    {
        lock (_lock)
        {
            if (_subscribers.TryGetValue(typeof(TEvent), out var subscriberList))
            {
                subscriberList.Remove(handler);
            }
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        lock (_lock)
        {
            _subscribers.Clear();
        }
    }


    private sealed class SubscriptionToken(Action unsubscribe) : IDisposable
    {
        private Action? _unsubscribe = unsubscribe;
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed) return;
            _unsubscribe?.Invoke();
            _unsubscribe = null;
            _disposed = true;
        }
    }
}

