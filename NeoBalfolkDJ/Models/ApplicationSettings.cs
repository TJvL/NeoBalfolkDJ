using System.Collections.Generic;

namespace NeoBalfolkDJ.Models;

/// <summary>
/// Application settings stored in user local application data.
/// Note: Kept as mutable class because settings are modified in-place throughout the app.
/// </summary>
public class ApplicationSettings
{
    public string MusicDirectoryPath { get; set; } = string.Empty;
    public int MaxQueueItems { get; set; } = 6;
    public int DelaySeconds { get; set; } = 30;
    public int PresentationDisplayCount { get; set; }
    public bool AutoQueueRandomTrack { get; set; }
    public bool AllowDuplicateTracksInQueue { get; set; } = true;
    public AppTheme Theme { get; set; } = AppTheme.Auto;

    // Main window state
    public double? WindowX { get; set; }
    public double? WindowY { get; set; }
    public double? WindowWidth { get; set; }
    public double? WindowHeight { get; set; }
    public bool IsMaximized { get; set; }

    // Presentation window states (keyed by index)
    public List<WindowState> PresentationWindowStates { get; init; } = [];
}

/// <summary>
/// Window position and size state.
/// </summary>
public class WindowState
{
    public double? X { get; set; }
    public double? Y { get; set; }
    public double? Width { get; set; }
    public double? Height { get; set; }
    public bool IsMaximized { get; init; }
}
