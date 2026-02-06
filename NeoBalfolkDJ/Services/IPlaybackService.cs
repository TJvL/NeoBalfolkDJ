using System;
using System.Threading.Tasks;
using NeoBalfolkDJ.Models;

namespace NeoBalfolkDJ.Services;

/// <summary>
/// Interface for audio playback services.
/// </summary>
public interface IPlaybackService : IDisposable
{
    /// <summary>
    /// Fired when playback time changes. Args: current time in milliseconds.
    /// </summary>
    event EventHandler<long>? TimeChanged;

    /// <summary>
    /// Fired when media length is known. Args: length in milliseconds.
    /// </summary>
    event EventHandler<long>? LengthChanged;

    /// <summary>
    /// Fired when playback reaches the end of the track.
    /// </summary>
    event EventHandler? EndReached;

    /// <summary>
    /// Fired when playback state changes. Args: true if playing, false otherwise.
    /// </summary>
    event EventHandler<bool>? PlayingChanged;

    /// <summary>
    /// Fired when a new track is loaded. Args: the loaded track.
    /// </summary>
    event EventHandler<Track>? TrackLoaded;

    /// <summary>
    /// Fired when track is cleared.
    /// </summary>
    event EventHandler? TrackCleared;

    /// <summary>
    /// Indicates if the service is properly initialized.
    /// </summary>
    bool IsInitialized { get; }

    /// <summary>
    /// Indicates if audio is currently playing.
    /// </summary>
    bool IsPlaying { get; }

    /// <summary>
    /// The currently loaded track, if any.
    /// </summary>
    Track? CurrentTrack { get; }

    /// <summary>
    /// Loads and starts playing a track.
    /// </summary>
    Task PlayTrackAsync(Track track);

    /// <summary>
    /// Toggles between play and pause states.
    /// </summary>
    Task PlayPauseAsync();

    /// <summary>
    /// Resumes playback of current track.
    /// </summary>
    Task PlayAsync();

    /// <summary>
    /// Pauses playback.
    /// </summary>
    Task PauseAsync();

    /// <summary>
    /// Stops playback and resets to beginning of track.
    /// </summary>
    Task StopAsync();

    /// <summary>
    /// Clears the current track and stops playback.
    /// </summary>
    Task ClearTrackAsync();

    /// <summary>
    /// Seeks to a specific position in milliseconds.
    /// </summary>
    Task SeekAsync(long positionMs);

    /// <summary>
    /// Restarts the current track from the beginning.
    /// If playing, continues playing from start. If paused, resets to start but stays paused.
    /// </summary>
    Task RestartAsync();
}
