using NeoBalfolkDJ.Models;

namespace NeoBalfolkDJ.Messaging.Commands;

/// <summary>
/// Command to add a track to the queue.
/// </summary>
public sealed record AddTrackToQueueCommand(Track Track);

