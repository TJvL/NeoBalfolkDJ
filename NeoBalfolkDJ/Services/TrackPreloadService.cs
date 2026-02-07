using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NeoBalfolkDJ.Models;

namespace NeoBalfolkDJ.Services;

/// <summary>
/// Service that pre-verifies track files are accessible asynchronously to prevent UI freezing.
/// LibVLC handles actual I/O efficiently, so we just verify file accessibility ahead of time.
/// Maintains a 2-slot cache for current and next track paths only.
/// </summary>
public sealed class TrackPreloadService(ILoggingService logger) : ITrackPreloadService, IDisposable
{
    private readonly ILoggingService _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private string? _currentTrackPath;
    private string? _nextTrackPath;
    private readonly Lock _lock = new();
    private bool _disposed;

    /// <summary>
    /// Pre-verifies a track file is accessible asynchronously into the "next" slot.
    /// </summary>
    /// <param name="track">The track to verify</param>
    /// <returns>True if file is accessible, false otherwise</returns>
    public async Task<bool> PreloadAsync(Track track)
    {
        if (_disposed) return false;

        if (string.IsNullOrEmpty(track.FilePath))
        {
            _logger.Warning("Attempted to preload track with empty path");
            return false;
        }

        try
        {
            _logger.Debug($"Verifying track accessibility: {track.Artist} - {track.Title}");

            // Verify file exists and is readable by opening it briefly
            await using var stream = new FileStream(
                track.FilePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 4096,
                useAsync: true);

            // Just verify we can access the file - no need to read content
            // The file length check ensures the file is accessible
            _ = stream.Length;

            lock (_lock)
            {
                _nextTrackPath = track.FilePath;
            }

            _logger.Debug($"Track verified: {track.Artist} - {track.Title}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to verify track: {track.FilePath}", ex);
            return false;
        }
    }

    /// <summary>
    /// Gets a dummy byte array for API compatibility.
    /// The actual loading is done by LibVLC from the file path.
    /// </summary>
    /// <param name="track">The track to check</param>
    /// <returns>An empty byte array if track is verified, null otherwise</returns>
    public byte[]? GetCachedData(Track track)
    {
        if (string.IsNullOrEmpty(track.FilePath))
            return null;

        lock (_lock)
        {
            if (_currentTrackPath == track.FilePath || _nextTrackPath == track.FilePath)
                return Array.Empty<byte>(); // Return empty array as marker that track is verified
        }

        return null;
    }

    /// <summary>
    /// Promotes the next track to current, clearing the next slot.
    /// Call this when starting playback of the next track.
    /// </summary>
    public void PromoteNextToCurrent()
    {
        if (_disposed) return;

        lock (_lock)
        {
            _currentTrackPath = _nextTrackPath;
            _nextTrackPath = null;
        }

        _logger.Debug("Promoted next track to current");
    }

    /// <summary>
    /// Sets the current track path directly (used when loading first track).
    /// </summary>
    public void SetCurrentTrackData(Track track, byte[] _)
    {
        lock (_lock)
        {
            _currentTrackPath = track.FilePath;
        }
    }

    /// <summary>
    /// Clears all cached paths.
    /// </summary>
    public void Clear()
    {
        if (_disposed) return;

        lock (_lock)
        {
            _currentTrackPath = null;
            _nextTrackPath = null;
        }

        _logger.Debug("Cleared all track cache");
    }

    /// <summary>
    /// Checks if a track has been verified accessible.
    /// </summary>
    public bool IsCached(Track track)
    {
        if (string.IsNullOrEmpty(track.FilePath))
            return false;

        lock (_lock)
        {
            return _currentTrackPath == track.FilePath || _nextTrackPath == track.FilePath;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        lock (_lock)
        {
            _currentTrackPath = null;
            _nextTrackPath = null;
        }
    }
}
