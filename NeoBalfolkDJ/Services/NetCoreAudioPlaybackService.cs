using System;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using NetCoreAudio;
using NeoBalfolkDJ.Models;
using Timer = System.Timers.Timer;

namespace NeoBalfolkDJ.Services;

/// <summary>
/// Playback service implementation using NetCoreAudio.
/// Lightweight cross-platform audio player.
/// </summary>
public class NetCoreAudioPlaybackService : IPlaybackService
{
    private readonly ILoggingService _logger;
    private readonly Player _player;
    private readonly SemaphoreSlim _playbackLock = new(1, 1);
    private readonly Timer _progressTimer;
    private bool _disposed;
    private bool _needsRestart; // True when stopped at beginning, needs Play() instead of Resume()
    private long _currentTimeMs;
    private long _durationMs;

    public event EventHandler<long>? TimeChanged;
    public event EventHandler<long>? LengthChanged;
    public event EventHandler? EndReached;
    public event EventHandler<bool>? PlayingChanged;
    public event EventHandler<Track>? TrackLoaded;
    public event EventHandler? TrackCleared;

    public bool IsInitialized => true;
    public bool IsPlaying { get; private set; }

    public Track? CurrentTrack { get; private set; }

    public NetCoreAudioPlaybackService(ILoggingService logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _player = new Player();
        _player.PlaybackFinished += OnPlaybackFinished;

        // Timer to track progress (NetCoreAudio doesn't provide time events)
        _progressTimer = new Timer(250); // Update every 250ms
        _progressTimer.Elapsed += OnProgressTimerElapsed;

        _logger.Info("NetCoreAudioPlaybackService: Initialized successfully");
    }

    private void OnPlaybackFinished(object? sender, EventArgs e)
    {
        IsPlaying = false;
        PlayingChanged?.Invoke(this, false);
        EndReached?.Invoke(this, EventArgs.Empty);
        _progressTimer.Stop();
    }

    private void OnProgressTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        if (IsPlaying && _durationMs > 0)
        {
            _currentTimeMs += 250;
            if (_currentTimeMs > _durationMs)
            {
                _currentTimeMs = _durationMs;
            }
            TimeChanged?.Invoke(this, _currentTimeMs);
        }
    }

    public async Task PlayTrackAsync(Track track)
    {
        await _playbackLock.WaitAsync();
        try
        {
            // Stop current playback
            if (IsPlaying)
            {
                await _player.Stop();
                _progressTimer.Stop();
            }

            CurrentTrack = track;
            _currentTimeMs = 0;

            // Get duration from track metadata if available, otherwise estimate
            _durationMs = (long)track.Length.TotalMilliseconds;

            _logger.Debug($"NetCoreAudioPlaybackService: Loading track: {track.Artist} - {track.Title}");

            await _player.Play(track.FilePath);
            IsPlaying = true;

            // Notify listeners
            TrackLoaded?.Invoke(this, track);
            if (_durationMs > 0)
            {
                LengthChanged?.Invoke(this, _durationMs);
            }
            PlayingChanged?.Invoke(this, true);

            _progressTimer.Start();

            _logger.Debug($"NetCoreAudioPlaybackService: Started playback: {track.Artist} - {track.Title}");
        }
        catch (Exception ex)
        {
            _logger.Error($"NetCoreAudioPlaybackService: Error playing track: {track.FilePath}", ex);
            throw;
        }
        finally
        {
            _playbackLock.Release();
        }
    }

    public async Task PlayPauseAsync()
    {
        await _playbackLock.WaitAsync();
        try
        {
            if (IsPlaying)
            {
                await _player.Pause();
                IsPlaying = false;
                _progressTimer.Stop();
                PlayingChanged?.Invoke(this, false);
            }
            else if (CurrentTrack != null)
            {
                if (_needsRestart)
                {
                    // Track was stopped, need to play from beginning
                    await _player.Play(CurrentTrack.FilePath);
                    _needsRestart = false;
                }
                else
                {
                    // Track was paused, can resume
                    await _player.Resume();
                }
                IsPlaying = true;
                _progressTimer.Start();
                PlayingChanged?.Invoke(this, true);
            }
        }
        finally
        {
            _playbackLock.Release();
        }
    }

    public async Task PlayAsync()
    {
        await _playbackLock.WaitAsync();
        try
        {
            if (CurrentTrack != null && !IsPlaying)
            {
                if (_needsRestart)
                {
                    // Track was stopped, need to play from beginning
                    await _player.Play(CurrentTrack.FilePath);
                    _needsRestart = false;
                }
                else
                {
                    // Track was paused, can resume
                    await _player.Resume();
                }
                IsPlaying = true;
                _progressTimer.Start();
                PlayingChanged?.Invoke(this, true);
            }
        }
        finally
        {
            _playbackLock.Release();
        }
    }

    public async Task PauseAsync()
    {
        await _playbackLock.WaitAsync();
        try
        {
            if (IsPlaying)
            {
                await _player.Pause();
                IsPlaying = false;
                _progressTimer.Stop();
                PlayingChanged?.Invoke(this, false);
            }
        }
        finally
        {
            _playbackLock.Release();
        }
    }

    public async Task StopAsync()
    {
        await _playbackLock.WaitAsync();
        try
        {
            await _player.Stop();
            IsPlaying = false;
            _currentTimeMs = 0;
            _progressTimer.Stop();
            PlayingChanged?.Invoke(this, false);
            TimeChanged?.Invoke(this, 0);
        }
        finally
        {
            _playbackLock.Release();
        }
    }

    public async Task ClearTrackAsync()
    {
        await _playbackLock.WaitAsync();
        try
        {
            await _player.Stop();
            IsPlaying = false;
            CurrentTrack = null;
            _currentTimeMs = 0;
            _durationMs = 0;
            _progressTimer.Stop();

            TrackCleared?.Invoke(this, EventArgs.Empty);
        }
        finally
        {
            _playbackLock.Release();
        }
    }

    public Task SeekAsync(long positionMs)
    {
        // NetCoreAudio has limited seek support
        // This is a limitation of the library
        _logger.Warning("NetCoreAudioPlaybackService: Seek not fully supported");
        _currentTimeMs = positionMs;
        TimeChanged?.Invoke(this, _currentTimeMs);
        return Task.CompletedTask;
    }

    public async Task RestartAsync()
    {
        await _playbackLock.WaitAsync();
        try
        {
            if (CurrentTrack == null) return;

            var wasPlaying = IsPlaying;
            var track = CurrentTrack;

            // Stop current playback
            await _player.Stop();
            _progressTimer.Stop();
            _currentTimeMs = 0;
            TimeChanged?.Invoke(this, 0);

            if (wasPlaying)
            {
                // Restart playback from beginning
                await _player.Play(track.FilePath);
                IsPlaying = true;
                _needsRestart = false;
                _progressTimer.Start();
                PlayingChanged?.Invoke(this, true);
            }
            else
            {
                // Stay paused at beginning - need Play() not Resume() when user presses play
                IsPlaying = false;
                _needsRestart = true;
                PlayingChanged?.Invoke(this, false);
            }
        }
        finally
        {
            _playbackLock.Release();
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _progressTimer.Stop();
        _progressTimer.Elapsed -= OnProgressTimerElapsed;
        _progressTimer.Dispose();

        _player.PlaybackFinished -= OnPlaybackFinished;
        _player.Stop().GetAwaiter().GetResult();

        _playbackLock.Dispose();

        _logger.Info("NetCoreAudioPlaybackService: Disposed");
    }
}
