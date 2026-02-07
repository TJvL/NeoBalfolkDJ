namespace NeoBalfolkDJ.Messaging.Commands;

/// <summary>
/// Command to add a message marker to the queue.
/// </summary>
public sealed record AddMessageMarkerCommand(string Message, int? DelaySeconds);

