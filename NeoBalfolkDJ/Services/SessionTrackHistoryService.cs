using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using NeoBalfolkDJ.Models;

namespace NeoBalfolkDJ.Services;

/// <summary>
/// Tracks which songs have been played in the current session.
/// Used to prevent duplicate tracks when the setting is enabled.
/// </summary>
public class SessionTrackHistoryService
{
    private readonly HashSet<string> _playedTrackPaths = new();
    private readonly ObservableCollection<Track> _playedTracks = new();
    
    /// <summary>
    /// Observable collection of played tracks for UI binding
    /// </summary>
    public ReadOnlyObservableCollection<Track> PlayedTracks { get; }

    /// <summary>
    /// Event raised when history changes
    /// </summary>
    public event EventHandler? HistoryChanged;

    public SessionTrackHistoryService()
    {
        PlayedTracks = new ReadOnlyObservableCollection<Track>(_playedTracks);
    }
    
    /// <summary>
    /// Records a track as having been played in this session.
    /// </summary>
    public void AddPlayedTrack(Track track)
    {
        _playedTrackPaths.Add(track.FilePath);
        _playedTracks.Add(track);
        LoggingService.Debug($"Track added to session history: {track.Artist} - {track.Title}");
        HistoryChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Checks if a track has already been played in this session.
    /// </summary>
    public bool HasBeenPlayed(Track track)
    {
        return _playedTrackPaths.Contains(track.FilePath);
    }

    /// <summary>
    /// Clears the session history (e.g., when music directory changes).
    /// </summary>
    public void Clear()
    {
        var count = _playedTrackPaths.Count;
        _playedTrackPaths.Clear();
        _playedTracks.Clear();
        LoggingService.Debug($"Session track history cleared: {count} tracks removed");
        HistoryChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Gets the number of tracks in the history.
    /// </summary>
    public int Count => _playedTracks.Count;

    /// <summary>
    /// Gets the total duration of all played tracks
    /// </summary>
    public TimeSpan TotalDuration => TimeSpan.FromTicks(_playedTracks.Sum(t => t.Length.Ticks));

    /// <summary>
    /// Gets the total duration formatted as string
    /// </summary>
    public string TotalDurationFormatted
    {
        get
        {
            var duration = TotalDuration;
            if (duration.TotalHours >= 1)
            {
                return $"{(int)duration.TotalHours}:{duration.Minutes:D2}:{duration.Seconds:D2}";
            }
            return $"{(int)duration.TotalMinutes}:{duration.Seconds:D2}";
        }
    }

    /// <summary>
    /// Exports the history to a JSON file
    /// </summary>
    public async Task ExportToJsonAsync(string filePath)
    {
        var exportData = new
        {
            tracks = _playedTracks.Select(t => new
            {
                dance = t.Dance,
                artist = t.Artist,
                title = t.Title,
                lengthSeconds = (int)t.Length.TotalSeconds
            }).ToArray(),
            totalLengthSeconds = (int)TotalDuration.TotalSeconds
        };

        var json = JsonSerializer.Serialize(exportData, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        await File.WriteAllTextAsync(filePath, json);
        LoggingService.Info($"History exported to: {filePath}");
    }
}
