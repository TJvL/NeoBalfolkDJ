namespace NeoBalfolkDJ.Messaging.Events;

/// <summary>
/// Event raised when the queue view toggles between queue mode and history mode.
/// </summary>
public sealed record HistoryModeChangedEvent(bool IsHistoryMode);

