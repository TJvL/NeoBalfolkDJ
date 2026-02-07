using NeoBalfolkDJ.Models;

namespace NeoBalfolkDJ.Services;

/// <summary>
/// Service for managing presentation display windows.
/// </summary>
public interface IPresentationDisplayService
{
    /// <summary>
    /// Updates the number of presentation windows to display.
    /// </summary>
    void UpdateDisplayCount(int count);

    /// <summary>
    /// Updates the current track display.
    /// </summary>
    void UpdateCurrentTrack(Track? track);

    /// <summary>
    /// Shows a stop marker on the display.
    /// </summary>
    void ShowStopMarker();

    /// <summary>
    /// Shows a delay marker with countdown.
    /// </summary>
    void ShowDelayMarker(long durationMs);

    /// <summary>
    /// Shows a message marker.
    /// </summary>
    void ShowMessageMarker(string message, long? durationMs);

    /// <summary>
    /// Updates the progress display.
    /// </summary>
    void UpdateProgress(double currentMs, double totalMs);

    /// <summary>
    /// Updates the next item display.
    /// </summary>
    void UpdateNextItem(IQueueItem? item);

    /// <summary>
    /// Saves the current window states.
    /// </summary>
    void SaveWindowStates();

    /// <summary>
    /// Clears all presentation displays.
    /// </summary>
    void Clear();
}


