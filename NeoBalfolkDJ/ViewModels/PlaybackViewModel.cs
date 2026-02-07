using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeoBalfolkDJ.Messaging;
using NeoBalfolkDJ.Messaging.Commands;
using NeoBalfolkDJ.Messaging.Events;
using NeoBalfolkDJ.Models;
using NeoBalfolkDJ.Services;

namespace NeoBalfolkDJ.ViewModels;

public partial class PlaybackViewModel : ViewModelBase, IDisposable
{
    private readonly IPlaybackService? _playbackService;
    private readonly ICommandBus? _commandBus;
    private readonly IEventAggregator? _eventAggregator;
    private bool _disposed;

    [ObservableProperty]
    private string _danceName = "No track playing...";

    [ObservableProperty]
    private string _artistName = "...";

    [ObservableProperty]
    private string _trackTitle = "...";

    [ObservableProperty]
    private double _progress;

    [ObservableProperty]
    private double _duration;

    [ObservableProperty]
    private string _currentTime = "0:00";

    [ObservableProperty]
    private string _totalTime = "0:00";

    [ObservableProperty]
    private bool _isPlaying;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasTrack))]
    [NotifyPropertyChangedFor(nameof(CanRestart))]
    [NotifyPropertyChangedFor(nameof(CanPlay))]
    [NotifyPropertyChangedFor(nameof(CanNextOrClear))]
    [NotifyCanExecuteChangedFor(nameof(PlayPauseCommand))]
    [NotifyCanExecuteChangedFor(nameof(RestartCommand))]
    [NotifyCanExecuteChangedFor(nameof(NextOrClearCommand))]
    private Track? _currentTrack;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanPlay))]
    [NotifyPropertyChangedFor(nameof(CanNextOrClear))]
    [NotifyPropertyChangedFor(nameof(ShowNextIcon))]
    [NotifyCanExecuteChangedFor(nameof(PlayPauseCommand))]
    [NotifyCanExecuteChangedFor(nameof(NextOrClearCommand))]
    private bool _queueHasItems;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanNextOrClear))]
    [NotifyCanExecuteChangedFor(nameof(NextOrClearCommand))]
    private bool _isStopMarkerActive;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanNextOrClear))]
    [NotifyCanExecuteChangedFor(nameof(NextOrClearCommand))]
    private bool _isDelayActive;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanNextOrClear))]
    [NotifyCanExecuteChangedFor(nameof(NextOrClearCommand))]
    private bool _isMessageMode;

    public bool HasTrack => CurrentTrack != null;

    /// <summary>
    /// Play button enabled when queue has items OR a track is loaded
    /// </summary>
    public bool CanPlay => QueueHasItems || HasTrack;

    /// <summary>
    /// Restart button enabled when a track is loaded
    /// </summary>
    public bool CanRestart => HasTrack;

    /// <summary>
    /// Next/Clear button enabled when queue has items OR a track is loaded OR any marker is active
    /// </summary>
    public bool CanNextOrClear => QueueHasItems || HasTrack || IsStopMarkerActive || IsDelayActive || IsMessageMode;

    /// <summary>
    /// Show next icon when queue has items, otherwise show clear (X) icon
    /// </summary>
    public bool ShowNextIcon => QueueHasItems;

    // Note: PlaybackStartRequested, NextTrackRequested, TrackFinished replaced by ICommandBus/IEventAggregator

    /// <summary>
    /// Event raised when clear track confirmation is needed (View shows dialog)
    /// </summary>
    public event EventHandler? ClearConfirmationRequested;

    /// <summary>
    /// Event raised when restart confirmation is needed (View shows dialog)
    /// </summary>
    public event EventHandler? RestartConfirmationRequested;

    /// <summary>
    /// Event raised when skip/next confirmation is needed (View shows dialog)
    /// </summary>
    public event EventHandler? SkipConfirmationRequested;

    /// <summary>
    /// Design-time constructor
    /// </summary>
    public PlaybackViewModel() : this(null!, null!, null!)
    {
        if (Design.IsDesignMode)
        {
            LoadDesignTimeData();
        }
    }

    public PlaybackViewModel(IPlaybackService playbackService, ICommandBus commandBus, IEventAggregator eventAggregator)
    {
        _playbackService = playbackService;
        _commandBus = commandBus;
        _eventAggregator = eventAggregator;

        // Load design-time data for previewing in designer
        if (Design.IsDesignMode)
        {
            return;
        }

        if (_playbackService == null) return;

        // Subscribe to service events and marshal to UI thread
        _playbackService.TimeChanged += OnTimeChanged;
        _playbackService.LengthChanged += OnLengthChanged;
        _playbackService.EndReached += OnEndReached;
        _playbackService.PlayingChanged += OnPlayingChanged;
        _playbackService.TrackLoaded += OnTrackLoaded;
        _playbackService.TrackCleared += OnTrackCleared;
    }

    private void LoadDesignTimeData()
    {
        CurrentTrack = new Track("Mazurka", "Sample Artist", "Sample Track Title", TimeSpan.FromMinutes(3.75), "");
        DanceName = "Mazurka";
        ArtistName = "Sample Artist";
        TrackTitle = "Sample Track Title";
        CurrentTime = "1:23";
        TotalTime = "3:45";
        Progress = 83000;
        Duration = 225000;
    }

    private void OnTimeChanged(object? sender, long timeMs)
    {
        Dispatcher.UIThread.Post(() =>
        {
            Progress = timeMs;
            var time = TimeSpan.FromMilliseconds(timeMs);
            CurrentTime = $"{(int)time.TotalMinutes}:{time.Seconds:D2}";
        });
    }

    private void OnLengthChanged(object? sender, long lengthMs)
    {
        Dispatcher.UIThread.Post(() =>
        {
            Duration = lengthMs;
            var time = TimeSpan.FromMilliseconds(lengthMs);
            TotalTime = $"{(int)time.TotalMinutes}:{time.Seconds:D2}";
        });
    }

    private void OnEndReached(object? sender, EventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            IsPlaying = false;
            // Publish event that track finished
            if (CurrentTrack != null)
            {
                _eventAggregator?.Publish(new TrackFinishedEvent(CurrentTrack));
            }
            // Auto-advance to next track
            _commandBus?.SendAsync(new PlayNextTrackCommand());
        });
    }

    private void OnPlayingChanged(object? sender, bool isPlaying)
    {
        Dispatcher.UIThread.Post(() =>
        {
            IsPlaying = isPlaying;
            // Note: Progress reset is handled by TimeChanged event from StopAsync
            // Don't reset here as this also fires on pause
        });
    }

    private void OnTrackLoaded(object? sender, Track track)
    {
        Dispatcher.UIThread.Post(() =>
        {
            IsMessageMode = false;
            IsStopMarkerActive = false;
            IsDelayActive = false;
            CurrentTrack = track;
            DanceName = track.Dance;
            ArtistName = track.Artist;
            TrackTitle = track.Title;
            
            // Publish event that track started
            _eventAggregator?.Publish(new TrackStartedEvent(track));
        });
    }

    private void OnTrackCleared(object? sender, EventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            IsMessageMode = false;
            IsStopMarkerActive = false;
            IsDelayActive = false;
            CurrentTrack = null;
            DanceName = "No track playing...";
            ArtistName = "...";
            TrackTitle = "...";
            Progress = 0;
            Duration = 0;
            CurrentTime = "0:00";
            TotalTime = "0:00";
            IsPlaying = false;
        });
    }

    /// <summary>
    /// Displays the stop marker state in the playback view.
    /// Shows "Stop" as dance name and single space for artist/title to retain spacing.
    /// </summary>
    public void ShowStopMarker()
    {
        IsMessageMode = false;
        IsStopMarkerActive = true;
        IsDelayActive = false;
        CurrentTrack = null;
        DanceName = "Stop";
        ArtistName = " ";
        TrackTitle = " ";
        Progress = 0;
        Duration = 0;
        CurrentTime = "0:00";
        TotalTime = "0:00";
        IsPlaying = false;
    }

    /// <summary>
    /// Displays the delay marker state with total duration.
    /// Shows "Delay" as dance name and progress bar will count up.
    /// </summary>
    public void ShowDelayMarker(long totalMs)
    {
        IsMessageMode = false;
        IsStopMarkerActive = false;
        IsDelayActive = true;
        CurrentTrack = null;
        DanceName = "Delay";
        ArtistName = " ";
        TrackTitle = " ";
        Progress = 0;
        Duration = totalMs;
        CurrentTime = "0:00";
        var total = TimeSpan.FromMilliseconds(totalMs);
        TotalTime = $"{(int)total.TotalMinutes}:{total.Seconds:D2}";
        IsPlaying = true; // Show as "playing" since it's counting
    }

    /// <summary>
    /// Displays the message marker state.
    /// If totalMs is provided, shows countdown like delay. Otherwise, behaves like stop.
    /// </summary>
    public void ShowMessageMarker(string message, long? totalMs = null)
    {
        IsMessageMode = true;
        IsDelayActive = false;
        CurrentTrack = null;
        DanceName = message;
        ArtistName = " ";
        TrackTitle = " ";

        if (totalMs.HasValue)
        {
            // Message with delay - show countdown
            IsStopMarkerActive = false;
            Progress = 0;
            Duration = totalMs.Value;
            CurrentTime = "0:00";
            var total = TimeSpan.FromMilliseconds(totalMs.Value);
            TotalTime = $"{(int)total.TotalMinutes}:{total.Seconds:D2}";
            IsPlaying = true; // Show as "playing" since it's counting
        }
        else
        {
            // Message without delay - behave like stop
            IsStopMarkerActive = true;
            Progress = 0;
            Duration = 0;
            CurrentTime = "0:00";
            TotalTime = "0:00";
            IsPlaying = false;
        }
    }

    /// <summary>
    /// Updates the delay progress display.
    /// </summary>
    public void UpdateDelayProgress(long elapsedMs, long totalMs)
    {
        Progress = elapsedMs;
        var elapsed = TimeSpan.FromMilliseconds(elapsedMs);
        CurrentTime = $"{(int)elapsed.TotalMinutes}:{elapsed.Seconds:D2}";
    }

    /// <summary>
    /// Plays a track. All LibVLC operations happen on background thread.
    /// </summary>
    public Task PlayTrackAsync(Track track)
    {
        return _playbackService?.PlayTrackAsync(track) ?? Task.CompletedTask;
    }

    /// <summary>
    /// Clears the current track and stops playback.
    /// </summary>
    public Task ClearTrackAsync()
    {
        return _playbackService?.ClearTrackAsync() ?? Task.CompletedTask;
    }

    /// <summary>
    /// Stops playback without clearing track state. Used for stop markers.
    /// </summary>
    public Task StopPlaybackAsync()
    {
        return _playbackService?.StopAsync() ?? Task.CompletedTask;
    }

    /// <summary>
    /// Synchronous clear for simple cases
    /// </summary>
    public void ClearTrack()
    {
        _ = ClearTrackAsync();
    }

    [RelayCommand(CanExecute = nameof(CanPlay))]
    private async Task PlayPauseAsync()
    {
        // If stop marker is active and queue has items, advance to next
        if (IsStopMarkerActive && QueueHasItems)
        {
            IsStopMarkerActive = false;
            IsDelayActive = false;
            _commandBus?.SendAsync(new PlayNextTrackCommand());
            return;
        }

        // If no track is loaded but queue has items, request playback start
        if (!HasTrack && QueueHasItems)
        {
            _commandBus?.SendAsync(new PlayNextTrackCommand());
            return;
        }

        if (_playbackService != null)
        {
            await _playbackService.PlayPauseAsync();
        }
    }

    [RelayCommand(CanExecute = nameof(CanRestart))]
    private void Restart()
    {
        RestartConfirmationRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Called by View after user confirms restart.
    /// </summary>
    public async Task ConfirmRestartAsync()
    {
        if (!HasTrack || _playbackService == null) return;
        await _playbackService.RestartAsync();
    }

    [RelayCommand(CanExecute = nameof(CanNextOrClear))]
    private void NextOrClear()
    {
        if (QueueHasItems)
        {
            // Queue has items - request confirmation before skipping
            SkipConfirmationRequested?.Invoke(this, EventArgs.Empty);
        }
        else if (HasTrack)
        {
            // No queue items but track is loaded - request confirmation before clearing
            ClearConfirmationRequested?.Invoke(this, EventArgs.Empty);
        }
        else if (IsStopMarkerActive || IsDelayActive || IsMessageMode)
        {
            // Marker is active but no queue items - clear to empty state
            IsStopMarkerActive = false;
            IsDelayActive = false;
            IsMessageMode = false;
            DanceName = "No track playing...";
            ArtistName = "...";
            TrackTitle = "...";
            IsPlaying = false;
        }
    }

    /// <summary>
    /// Called by View after user confirms skipping to next.
    /// </summary>
    public void ConfirmSkip()
    {
        _commandBus?.SendAsync(new PlayNextTrackCommand());
    }

    /// <summary>
    /// Called by View after user confirms clearing the track.
    /// </summary>
    public void ConfirmClear()
    {
        _ = ClearTrackAsync();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (_playbackService != null)
        {
            _playbackService.TimeChanged -= OnTimeChanged;
            _playbackService.LengthChanged -= OnLengthChanged;
            _playbackService.EndReached -= OnEndReached;
            _playbackService.PlayingChanged -= OnPlayingChanged;
            _playbackService.TrackLoaded -= OnTrackLoaded;
            _playbackService.TrackCleared -= OnTrackCleared;

            _playbackService.Dispose();
        }
    }
}
