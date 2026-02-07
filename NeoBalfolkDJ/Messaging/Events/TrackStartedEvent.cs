using NeoBalfolkDJ.Models;

namespace NeoBalfolkDJ.Messaging.Events;

/// <summary>
/// Event raised when a track starts playing.
/// </summary>
public sealed record TrackStartedEvent(Track Track);

