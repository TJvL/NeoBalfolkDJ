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
    private readonly Player _player;
    private readonly SemaphoreSlim _playbackLock = new(1, 1);
    private readonly Timer _progressTimer;
    private bool _disposed;
    private Track? _currentTrack;
    private bool _isPlaying;
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
    public bool IsPlaying => _isPlaying;
    public Track? CurrentTrack => _currentTrack;

    public NetCoreAudioPlaybackService()
    {
        _player = new Player();
        _player.PlaybackFinished += OnPlaybackFinished;

        // Timer to track progress (NetCoreAudio doesn't provide time events)
        _progressTimer = new Timer(250); // Update every 250ms
        _progressTimer.Elapsed += OnProgressTimerElapsed;
        
        LoggingService.Info("NetCoreAudioPlaybackService: Initialized successfully");
    }

    private void OnPlaybackFinished(object? sender, EventArgs e)
    {
        _isPlaying = false;
        PlayingChanged?.Invoke(this, false);
        EndReached?.Invoke(this, EventArgs.Empty);
        _progressTimer.Stop();
    }

    private void OnProgressTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        if (_isPlaying && _durationMs > 0)
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
            if (_isPlaying)
            {
                await _player.Stop();
                _progressTimer.Stop();
            }

            _currentTrack = track;
            _currentTimeMs = 0;
            
            // Get duration from track metadata if available, otherwise estimate
            _durationMs = (long)track.Length.TotalMilliseconds;
            
            LoggingService.Debug($"NetCoreAudioPlaybackService: Loading track: {track.Artist} - {track.Title}");

            await _player.Play(track.FilePath);
            _isPlaying = true;
            
            // Notify listeners
            TrackLoaded?.Invoke(this, track);
            if (_durationMs > 0)
            {
                LengthChanged?.Invoke(this, _durationMs);
            }
            PlayingChanged?.Invoke(this, true);
            
            _progressTimer.Start();
            
            LoggingService.Debug($"NetCoreAudioPlaybackService: Started playback: {track.Artist} - {track.Title}");
        }
        catch (Exception ex)
        {
            LoggingService.Error($"NetCoreAudioPlaybackService: Error playing track: {track.FilePath}", ex);
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
            if (_isPlaying)
            {
                await _player.Pause();
                _isPlaying = false;
                _progressTimer.Stop();
                PlayingChanged?.Invoke(this, false);
            }
            else if (_currentTrack != null)
            {
                if (_needsRestart)
                {
                    // Track was stopped, need to play from beginning
                    await _player.Play(_currentTrack.FilePath);
                    _needsRestart = false;
                }
                else
                {
                    // Track was paused, can resume
                    await _player.Resume();
                }
                _isPlaying = true;
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
            if (_currentTrack != null && !_isPlaying)
            {
                if (_needsRestart)
                {
                    // Track was stopped, need to play from beginning
                    await _player.Play(_currentTrack.FilePath);
                    _needsRestart = false;
                }
                else
                {
                    // Track was paused, can resume
                    await _player.Resume();
                }
                _isPlaying = true;
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
            if (_isPlaying)
            {
                await _player.Pause();
                _isPlaying = false;
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
            _isPlaying = false;
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
            _isPlaying = false;
            _currentTrack = null;
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
        LoggingService.Warning("NetCoreAudioPlaybackService: Seek not fully supported");
        _currentTimeMs = positionMs;
        TimeChanged?.Invoke(this, _currentTimeMs);
        return Task.CompletedTask;
    }

    public async Task RestartAsync()
    {
        await _playbackLock.WaitAsync();
        try
        {
            if (_currentTrack == null) return;

            var wasPlaying = _isPlaying;
            var track = _currentTrack;

            // Stop current playback
            await _player.Stop();
            _progressTimer.Stop();
            _currentTimeMs = 0;
            TimeChanged?.Invoke(this, 0);

            if (wasPlaying)
            {
                // Restart playback from beginning
                await _player.Play(track.FilePath);
                _isPlaying = true;
                _needsRestart = false;
                _progressTimer.Start();
                PlayingChanged?.Invoke(this, true);
            }
            else
            {
                // Stay paused at beginning - need Play() not Resume() when user presses play
                _isPlaying = false;
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
        
        LoggingService.Info("NetCoreAudioPlaybackService: Disposed");
    }
}
