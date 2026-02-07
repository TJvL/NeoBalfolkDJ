using System;
using System.Collections.ObjectModel;
using NeoBalfolkDJ.Models;

namespace NeoBalfolkDJ.Services;

/// <summary>
/// Central service that manages the in-memory index of all tracks.
/// </summary>
public interface ITrackStoreService
{
    /// <summary>
    /// Read-only collection of all tracks in the store
    /// </summary>
    ReadOnlyObservableCollection<Track> Tracks { get; }

    /// <summary>
    /// Gets the currently configured music directory path
    /// </summary>
    string MusicDirectoryPath { get; }

    /// <summary>
    /// Event raised when a track is added to the store
    /// </summary>
    event EventHandler<Track>? TrackAdded;

    /// <summary>
    /// Event raised when a track is removed from the store
    /// </summary>
    event EventHandler<Track>? TrackRemoved;

    /// <summary>
    /// Event raised when all tracks are reloaded
    /// </summary>
    event EventHandler? TracksReloaded;

    /// <summary>
    /// Gets a random track from the store, or null if the store is empty
    /// </summary>
    Track? GetRandomTrack();

    /// <summary>
    /// Gets a random track from the store that is not in the excluded set.
    /// </summary>
    Track? GetRandomTrackExcluding(Func<Track, bool> isExcluded);

    /// <summary>
    /// Gets a track by its file path
    /// </summary>
    Track? GetTrackByFilePath(string filePath);

    /// <summary>
    /// Sets the music directory and loads all tracks from it.
    /// </summary>
    void SetMusicDirectory(string directoryPath);
}

