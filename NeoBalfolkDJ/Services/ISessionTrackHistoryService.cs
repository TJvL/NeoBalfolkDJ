using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using NeoBalfolkDJ.Models;

namespace NeoBalfolkDJ.Services;

/// <summary>
/// Tracks which songs have been played in the current session.
/// </summary>
public interface ISessionTrackHistoryService
{
    /// <summary>
    /// Observable collection of played tracks for UI binding
    /// </summary>
    ReadOnlyObservableCollection<Track> PlayedTracks { get; }

    /// <summary>
    /// Gets the total duration of all played tracks formatted as string
    /// </summary>
    string TotalDurationFormatted { get; }

    /// <summary>
    /// Event raised when history changes
    /// </summary>
    event EventHandler? HistoryChanged;

    /// <summary>
    /// Records a track as having been played in this session.
    /// </summary>
    void AddPlayedTrack(Track track);

    /// <summary>
    /// Checks if a track has already been played in this session.
    /// </summary>
    bool HasBeenPlayed(Track track);

    /// <summary>
    /// Clears the session history.
    /// </summary>
    void Clear();

    /// <summary>
    /// Exports the history to a file.
    /// </summary>
    Task ExportToFileAsync(string filePath);
}

