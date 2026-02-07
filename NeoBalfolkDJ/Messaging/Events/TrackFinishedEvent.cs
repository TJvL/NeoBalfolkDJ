using NeoBalfolkDJ.Models;

namespace NeoBalfolkDJ.Messaging.Events;

/// <summary>
/// Event raised when a track finishes playing naturally (reached the end).
/// </summary>
public sealed record TrackFinishedEvent(Track Track);

