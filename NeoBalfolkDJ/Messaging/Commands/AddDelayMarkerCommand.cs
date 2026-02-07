namespace NeoBalfolkDJ.Messaging.Commands;

/// <summary>
/// Command to add a delay marker to the queue.
/// </summary>
public sealed record AddDelayMarkerCommand(int DelaySeconds);
