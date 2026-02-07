namespace NeoBalfolkDJ.Messaging.Events;

/// <summary>
/// Event raised when the playback queue contents change.
/// </summary>
public sealed record QueueChangedEvent(int ItemCount, bool HasManualItems, bool HasAutoQueuedItem);

