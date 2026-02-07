namespace NeoBalfolkDJ.Messaging.Events;

/// <summary>
/// The type of view being navigated to.
/// </summary>
public enum ViewType
{
    TrackList,
    Settings,
    Help
}

/// <summary>
/// Event raised when navigation changes (command result).
/// </summary>
public sealed record NavigationChangedEvent(ViewType View);

