using System;

namespace NeoBalfolkDJ.Messaging;

/// <summary>
/// Event aggregator for publishing and subscribing to events.
/// Events represent facts that have occurred (past tense).
/// </summary>
public interface IEventAggregator
{
    /// <summary>
    /// Publishes an event to all subscribers.
    /// The event is marshaled to the UI thread before delivery.
    /// </summary>
    void Publish<TEvent>(TEvent evt) where TEvent : class;

    /// <summary>
    /// Subscribes to events of type TEvent.
    /// </summary>
    /// <returns>A disposable subscription token. Dispose to unsubscribe.</returns>
    IDisposable Subscribe<TEvent>(Action<TEvent> handler) where TEvent : class;
}

