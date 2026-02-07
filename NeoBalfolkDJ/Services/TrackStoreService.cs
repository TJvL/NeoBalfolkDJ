using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using NeoBalfolkDJ.Models;

namespace NeoBalfolkDJ.Services;

/// <summary>
/// Central service that manages the in-memory index of all tracks.
/// Monitors the configured music directory for changes and keeps the track list in sync.
/// </summary>
public sealed class TrackStoreService : ITrackStoreService, IDisposable
{
    private readonly ILoggingService _logger;
    private readonly IMusicScannerService _musicScanner;
    private readonly ObservableCollection<Track> _tracks = new();
    private FileSystemWatcher? _fileWatcher;
    private bool _disposed;

    /// <summary>
    /// Read-only collection of all tracks in the store
    /// </summary>
    public ReadOnlyObservableCollection<Track> Tracks { get; }

    /// <summary>
    /// Event raised when a track is added to the store
    /// </summary>
    public event EventHandler<Track>? TrackAdded;

    /// <summary>
    /// Event raised when a track is removed from the store
    /// </summary>
    public event EventHandler<Track>? TrackRemoved;

    /// <summary>
    /// Event raised when all tracks are reloaded
    /// </summary>
    public event EventHandler? TracksReloaded;

    public TrackStoreService(ILoggingService logger, IMusicScannerService musicScanner)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _musicScanner = musicScanner ?? throw new ArgumentNullException(nameof(musicScanner));
        Tracks = new ReadOnlyObservableCollection<Track>(_tracks);
    }

    ~TrackStoreService() => Dispose(disposing: false);

    /// <summary>
    /// Gets the currently configured music directory path
    /// </summary>
    public string MusicDirectoryPath { get; private set; } = string.Empty;

    /// <summary>
    /// Gets a random track from the store, or null if the store is empty
    /// </summary>
    public Track? GetRandomTrack()
    {
        if (_tracks.Count == 0)
            return null;

        var random = new Random();
        var index = random.Next(_tracks.Count);
        return _tracks[index];
    }

    /// <summary>
    /// Gets a random track from the store that is not in the excluded set.
    /// Returns null if all tracks are excluded or the store is empty.
    /// </summary>
    public Track? GetRandomTrackExcluding(Func<Track, bool> isExcluded)
    {
        if (_tracks.Count == 0)
            return null;

        var availableTracks = _tracks.Where(t => !isExcluded(t)).ToList();
        if (availableTracks.Count == 0)
            return null;

        var random = new Random();
        var index = random.Next(availableTracks.Count);
        return availableTracks[index];
    }

    /// <summary>
    /// Gets a track by its file path
    /// </summary>
    public Track? GetTrackByFilePath(string filePath)
    {
        return _tracks.FirstOrDefault(t => t.FilePath.Equals(filePath, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Sets the music directory and loads all tracks from it.
    /// Also starts watching the directory for changes.
    /// </summary>
    /// <param name="directoryPath">The path to the music directory</param>
    public void SetMusicDirectory(string directoryPath)
    {
        if (string.IsNullOrWhiteSpace(directoryPath))
        {
            _logger.Warning("Attempted to set empty music directory path");
            return;
        }

        MusicDirectoryPath = directoryPath;

        // Stop any existing file watcher
        StopFileWatcher();

        // Load all tracks from the directory
        ReloadTracks();

        // Start watching for changes
        StartFileWatcher();
    }

    /// <summary>
    /// Reloads all tracks from the current music directory
    /// </summary>
    private void ReloadTracks()
    {
        _tracks.Clear();

        if (string.IsNullOrWhiteSpace(MusicDirectoryPath) || !Directory.Exists(MusicDirectoryPath))
        {
            _logger.Warning($"Music directory does not exist: {MusicDirectoryPath}");
            TracksReloaded?.Invoke(this, EventArgs.Empty);
            return;
        }

        var scannedTracks = _musicScanner.ScanDirectory(MusicDirectoryPath);

        foreach (var track in scannedTracks)
        {
            _tracks.Add(track);
        }

        _logger.Info($"TrackStore loaded {_tracks.Count} tracks");
        TracksReloaded?.Invoke(this, EventArgs.Empty);
    }

    private void StartFileWatcher()
    {
        if (string.IsNullOrWhiteSpace(MusicDirectoryPath) || !Directory.Exists(MusicDirectoryPath))
        {
            return;
        }

        try
        {
            _fileWatcher = new FileSystemWatcher(MusicDirectoryPath)
            {
                Filter = "*.mp3",
                IncludeSubdirectories = true,
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.CreationTime
            };

            _fileWatcher.Created += OnFileCreated;
            _fileWatcher.Deleted += OnFileDeleted;
            _fileWatcher.Renamed += OnFileRenamed;
            _fileWatcher.EnableRaisingEvents = true;

            _logger.Info($"File watcher started for: {MusicDirectoryPath}");
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to start file watcher", ex);
        }
    }

    private void StopFileWatcher()
    {
        if (_fileWatcher != null)
        {
            _fileWatcher.EnableRaisingEvents = false;
            _fileWatcher.Created -= OnFileCreated;
            _fileWatcher.Deleted -= OnFileDeleted;
            _fileWatcher.Renamed -= OnFileRenamed;
            _fileWatcher.Dispose();
            _fileWatcher = null;
            _logger.Debug("File watcher stopped");
        }
    }

    private void OnFileCreated(object sender, FileSystemEventArgs e)
    {
        // Avoid adding duplicates
        if (GetTrackByFilePath(e.FullPath) != null)
            return;

        var track = ParseTrackFromFile(e.FullPath);
        if (track != null)
        {
            // Use Avalonia's dispatcher to update on UI thread
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                _tracks.Add(track);
                TrackAdded?.Invoke(this, track);
                _logger.Debug($"Track added via file watcher: {track.Artist} - {track.Title}");
            });
        }
    }

    private void OnFileDeleted(object sender, FileSystemEventArgs e)
    {
        var track = GetTrackByFilePath(e.FullPath);
        if (track != null)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                _tracks.Remove(track);
                TrackRemoved?.Invoke(this, track);
                _logger.Debug($"Track removed via file watcher: {track.Artist} - {track.Title}");
            });
        }
    }

    private void OnFileRenamed(object sender, RenamedEventArgs e)
    {
        // Handle rename as delete old + add new
        var oldTrack = GetTrackByFilePath(e.OldFullPath);
        if (oldTrack != null)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                _tracks.Remove(oldTrack);
                TrackRemoved?.Invoke(this, oldTrack);
            });
        }

        var newTrack = ParseTrackFromFile(e.FullPath);
        if (newTrack != null)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                _tracks.Add(newTrack);
                TrackAdded?.Invoke(this, newTrack);
                _logger.Debug($"Track renamed via file watcher: {newTrack.Artist} - {newTrack.Title}");
            });
        }
    }

    private static Track? ParseTrackFromFile(string filePath)
    {
        try
        {
            var fileName = Path.GetFileNameWithoutExtension(filePath);
            var parts = fileName.Split(" - ");

            if (parts.Length != 3)
                return null;

            var dance = parts[0].Trim();
            var artist = parts[1].Trim();
            var title = parts[2].Trim();

            if (string.IsNullOrWhiteSpace(dance) ||
                string.IsNullOrWhiteSpace(artist) ||
                string.IsNullOrWhiteSpace(title))
            {
                return null;
            }

            // Read track duration
            var duration = TimeSpan.Zero;
            try
            {
                using var file = TagLib.File.Create(filePath);
                duration = file.Properties.Duration;
            }
            catch
            {
                // Ignore duration read errors
            }

            return new Track(dance, artist, title, duration, filePath);
        }
        catch
        {
            return null;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (_disposed) return;
        _disposed = true;

        if (disposing)
        {
            StopFileWatcher();
            TrackAdded = null;
            TrackRemoved = null;
            TracksReloaded = null;
        }
    }
}
