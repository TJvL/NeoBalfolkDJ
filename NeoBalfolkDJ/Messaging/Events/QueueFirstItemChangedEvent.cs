using NeoBalfolkDJ.Models;

namespace NeoBalfolkDJ.Messaging.Events;

/// <summary>
/// Event raised when the first item in the queue changes.
/// This occurs when an item is added to an empty queue, or after the first item is dequeued.
/// </summary>
public sealed record QueueFirstItemChangedEvent(IQueueItem? FirstItem);

