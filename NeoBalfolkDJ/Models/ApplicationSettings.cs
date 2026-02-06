using System.Collections.Generic;

namespace NeoBalfolkDJ.Models;

public class ApplicationSettings
{
    public string MusicDirectoryPath { get; set; } = string.Empty;
    public int MaxQueueItems { get; set; } = 6;
    public int DelaySeconds { get; set; } = 30;
    public int PresentationDisplayCount { get; set; } = 0;
    public bool AutoQueueRandomTrack { get; set; } = false;
    public bool AllowDuplicateTracksInQueue { get; set; } = true;
    public AppTheme Theme { get; set; } = AppTheme.Auto;
    
    // Main window state
    public double? WindowX { get; set; }
    public double? WindowY { get; set; }
    public double? WindowWidth { get; set; }
    public double? WindowHeight { get; set; }
    public bool IsMaximized { get; set; } = false;
    
    // Presentation window states (keyed by index)
    public List<WindowState> PresentationWindowStates { get; set; } = new();
}

public class WindowState
{
    public double? X { get; set; }
    public double? Y { get; set; }
    public double? Width { get; set; }
    public double? Height { get; set; }
    public bool IsMaximized { get; set; } = false;
}
