using NeoBalfolkDJ.Models;

namespace NeoBalfolkDJ.Messaging.Events;

/// <summary>
/// Event raised when application settings change.
/// </summary>
public sealed record SettingsChangedEvent(ApplicationSettings Settings);

