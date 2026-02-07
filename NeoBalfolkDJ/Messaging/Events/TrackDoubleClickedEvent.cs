using NeoBalfolkDJ.Models;

namespace NeoBalfolkDJ.Messaging.Events;

/// <summary>
/// Event raised when a track is double-clicked in the track list.
/// </summary>
public sealed record TrackDoubleClickedEvent(Track Track);

