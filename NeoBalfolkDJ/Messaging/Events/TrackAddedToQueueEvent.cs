using NeoBalfolkDJ.Models;

namespace NeoBalfolkDJ.Messaging.Events;

/// <summary>
/// Event raised when a track is successfully added to the queue.
/// </summary>
public sealed record TrackAddedToQueueEvent(Track Track);
