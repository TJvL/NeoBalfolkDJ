using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeoBalfolkDJ.Messaging;
using NeoBalfolkDJ.Messaging.Commands;
using NeoBalfolkDJ.Messaging.Events;
using NeoBalfolkDJ.Models;
using NeoBalfolkDJ.Services;

namespace NeoBalfolkDJ.ViewModels;

public partial class QueueViewModel : ViewModelBase
{
    private readonly ILoggingService? _logger;
    private readonly ISettingsService? _settingsService;
    private readonly IEventAggregator? _eventAggregator;
    private readonly ICommandBus? _commandBus;

    private ITrackStoreService? _trackStore;
    private ISessionTrackHistoryService? _sessionHistory;
    private Track? _currentlyPlayingTrack;

    public ObservableCollection<IQueueItem> QueuedItems { get; } = new();

    public INotificationService? NotificationService { get; set; }

    // Note: FirstItemChanged replaced by QueueFirstItemChangedEvent via IEventAggregator
    // Note: HistoryModeChanged replaced by HistoryModeChangedEvent via IEventAggregator
    // Note: SettingsRequested replaced by ShowSettingsCommand
    // Note: RefreshAutoQueuedTrackRequested replaced by RefreshAutoQueueCommand

    [ObservableProperty]
    private IQueueItem? _selectedItem;

    [ObservableProperty]
    private string _queueFinishTime = "queue finishes at: --:--";

    /// <summary>
    /// Remaining time on the currently playing track in milliseconds
    /// </summary>
    private long _currentTrackRemainingMs;

    [ObservableProperty]
    private int _maxQueueItems = 6;

    [ObservableProperty]
    private int _delaySeconds = 30;

    [ObservableProperty]
    private bool _allowDuplicateTracksInQueue = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusText))]
    private bool _isHistoryMode;


    /// <summary>
    /// Returns true if the queue has any items
    /// </summary>
    public bool HasItems => QueuedItems.Count > 0;

    /// <summary>
    /// Returns true if the queue has any non-auto-queued items
    /// </summary>
    public bool HasManualItems => QueuedItems.Any(i => i is not AutoQueuedTrack);

    /// <summary>
    /// Returns true if the queue already has an auto-queued item
    /// </summary>
    public bool HasAutoQueuedItem => QueuedItems.Any(i => i is AutoQueuedTrack);

    /// <summary>
    /// Status text shown at the bottom - changes based on history mode
    /// </summary>
    public string StatusText => IsHistoryMode
        ? $"total played: {_sessionHistory?.TotalDurationFormatted ?? "0:00"}"
        : QueueFinishTime;

    /// <summary>
    /// History items for display in history mode
    /// </summary>
    public ReadOnlyObservableCollection<Track>? HistoryItems => _sessionHistory?.PlayedTracks;

    /// <summary>
    /// Design-time constructor
    /// </summary>
    public QueueViewModel() : this(null!, null!, null!, null!)
    {
        if (Design.IsDesignMode)
        {
            LoadDesignTimeData();
        }
    }

    public QueueViewModel(ILoggingService logger, ISettingsService settingsService, IEventAggregator eventAggregator, ICommandBus commandBus)
    {
        _logger = logger;
        _settingsService = settingsService;
        _eventAggregator = eventAggregator;
        _commandBus = commandBus;

        if (!Design.IsDesignMode && _settingsService != null)
        {
            LoadSettings();
        }

        UpdateFinishTime();

        // Monitor collection changes
        QueuedItems.CollectionChanged += OnQueuedItemsChanged;
    }

    private void OnQueuedItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(HasItems));
    }

    /// <summary>
    /// Sets the TrackStoreService for shuffle functionality
    /// </summary>
    public void SetTrackStore(ITrackStoreService trackStore)
    {
        _trackStore = trackStore;
    }

    /// <summary>
    /// Sets the session history service for tracking played tracks
    /// </summary>
    public void SetSessionHistory(ISessionTrackHistoryService sessionHistory)
    {
        _sessionHistory = sessionHistory;
        _sessionHistory.HistoryChanged += OnHistoryChanged;
        OnPropertyChanged(nameof(HistoryItems));
    }

    private void OnHistoryChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(StatusText));
        OnPropertyChanged(nameof(HistoryItems));
    }

    /// <summary>
    /// Updates the currently playing track (used for duplicate checking)
    /// </summary>
    public void SetCurrentlyPlayingTrack(Track? track)
    {
        _currentlyPlayingTrack = track;
    }

    private void LoadSettings()
    {
        var settings = _settingsService?.Load();
        if (settings == null) return;

        MaxQueueItems = settings.MaxQueueItems;
        DelaySeconds = settings.DelaySeconds;
        AllowDuplicateTracksInQueue = settings.AllowDuplicateTracksInQueue;
    }

    public void UpdateMaxQueueItems(int maxItems)
    {
        MaxQueueItems = maxItems;
    }

    public void UpdateAllowDuplicates(bool allow)
    {
        AllowDuplicateTracksInQueue = allow;
    }

    private void LoadDesignTimeData()
    {
        QueuedItems.Add(new Track("Mazurka", "Sample Artist", "Queued Track 1", TimeSpan.FromSeconds(195), ""));

        QueuedItems.Add(new StopMarker());

        QueuedItems.Add(new Track("Waltz", "Folk Band", "Queued Track 2", TimeSpan.FromSeconds(180), ""));
    }

    public void AddTrack(Track track)
    {
        // Remove any auto-queued items when manually adding
        RemoveAutoQueuedItems();

        if (QueuedItems.Count >= MaxQueueItems)
        {
            NotificationService?.ShowNotification($"Queue is full (max {MaxQueueItems} items)", NotificationSeverity.Warning);
            return;
        }

        // Check for duplicates if not allowed (queue, currently playing, and session history)
        if (!AllowDuplicateTracksInQueue && IsTrackDuplicate(track))
        {
            NotificationService?.ShowNotification("Track has already been played or is in the queue", NotificationSeverity.Warning);
            return;
        }

        var wasEmpty = QueuedItems.Count == 0;
        QueuedItems.Add(track);
        UpdateFinishTime();
        _logger?.Debug($"Track added to queue: {track.Artist} - {track.Title}");

        // Publish events
        _eventAggregator?.Publish(new TrackAddedToQueueEvent(track));
        _eventAggregator?.Publish(new QueueChangedEvent(QueuedItems.Count, HasManualItems, HasAutoQueuedItem));

        // If this is the first item, notify listeners
        if (wasEmpty)
        {
            _eventAggregator?.Publish(new QueueFirstItemChangedEvent(track));
        }
    }

    /// <summary>
    /// Checks if a track is a duplicate (in queue, currently playing, or already played in session)
    /// </summary>
    public bool IsTrackDuplicate(Track track)
    {
        // Check if in queue
        if (ContainsTrack(track))
            return true;

        // Check if currently playing
        if (_currentlyPlayingTrack != null && _currentlyPlayingTrack.Equals(track))
            return true;

        // Check session history
        if (_sessionHistory != null && _sessionHistory.HasBeenPlayed(track))
            return true;

        return false;
    }

    /// <summary>
    /// Checks if a track is already in the queue (including auto-queued tracks)
    /// </summary>
    private bool ContainsTrack(Track track)
    {
        return QueuedItems.Any(item => item switch
        {
            Track t => t.Equals(track),
            AutoQueuedTrack aq => aq.Track.Equals(track),
            _ => false
        });
    }

    /// <summary>
    /// Adds an auto-queued track that will be removed when any manual queue operation occurs
    /// </summary>
    public void AddAutoQueuedTrack(Track track)
    {
        if (QueuedItems.Count >= MaxQueueItems)
        {
            return; // Silently fail for auto-queue
        }

        var wasEmpty = QueuedItems.Count == 0;
        var autoQueued = new AutoQueuedTrack(track);
        QueuedItems.Add(autoQueued);
        UpdateFinishTime();
        _logger?.Debug($"Auto-queued track added: {track.Artist} - {track.Title}");

        if (wasEmpty)
        {
            _eventAggregator?.Publish(new QueueFirstItemChangedEvent(autoQueued));
        }
    }

    /// <summary>
    /// Removes all auto-queued items from the queue
    /// </summary>
    public void RemoveAutoQueuedItems()
    {
        var autoQueued = QueuedItems.OfType<AutoQueuedTrack>().ToList();
        foreach (var item in autoQueued)
        {
            QueuedItems.Remove(item);
            _logger?.Debug($"Auto-queued track removed: {item.Artist} - {item.Title}");
        }

        if (autoQueued.Count > 0)
        {
            UpdateFinishTime();
            _eventAggregator?.Publish(new QueueFirstItemChangedEvent(PeekFirst()));
        }
    }

    /// <summary>
    /// Pins an auto-queued track, converting it to a regular queued track
    /// </summary>
    public void PinAutoQueuedTrack(AutoQueuedTrack autoQueued)
    {
        var index = QueuedItems.IndexOf(autoQueued);
        if (index < 0) return;

        var track = autoQueued.Track;
        QueuedItems[index] = track;

        _logger?.Debug($"Auto-queued track pinned: {track.Artist} - {track.Title}");

        // Notify if this was the first item
        if (index == 0)
        {
            _eventAggregator?.Publish(new QueueFirstItemChangedEvent(track));
        }
    }

    /// <summary>
    /// Requests a refresh of the auto-queued track (picks a new random one)
    /// </summary>
    public void RefreshAutoQueuedTrack(AutoQueuedTrack autoQueued)
    {
        _commandBus?.SendAsync(new RefreshAutoQueueCommand());
    }

    /// <summary>
    /// Replaces an auto-queued track with a new one
    /// </summary>
    public void ReplaceAutoQueuedTrack(AutoQueuedTrack oldItem, Track newTrack)
    {
        var index = QueuedItems.IndexOf(oldItem);
        if (index < 0) return;

        var newAutoQueued = new AutoQueuedTrack(newTrack);
        QueuedItems[index] = newAutoQueued;
        UpdateFinishTime();

        _logger?.Debug($"Auto-queued track replaced: {oldItem.Title} -> {newTrack.Title}");

        // Notify if this was the first item
        if (index == 0)
        {
            _eventAggregator?.Publish(new QueueFirstItemChangedEvent(newAutoQueued));
        }
    }

    /// <summary>
    /// Removes and returns the first item from the queue, or null if empty
    /// </summary>
    public IQueueItem? DequeueNext()
    {
        if (QueuedItems.Count == 0)
            return null;

        var item = QueuedItems[0];
        QueuedItems.RemoveAt(0);
        UpdateFinishTime();

        switch (item)
        {
            case Track track:
                _logger?.Debug($"Track dequeued: {track.Artist} - {track.Title}");
                break;
            case StopMarker:
                _logger?.Debug("Stop marker dequeued");
                break;
            case DelayMarker delay:
                _logger?.Debug($"Delay marker dequeued: {delay.Duration.TotalSeconds} seconds");
                break;
        }

        // Notify about new first item (or null if queue is now empty)
        _eventAggregator?.Publish(new QueueFirstItemChangedEvent(PeekFirst()));

        return item;
    }

    /// <summary>
    /// Returns the first item without removing it, or null if empty
    /// </summary>
    public IQueueItem? PeekFirst()
    {
        return QueuedItems.Count > 0 ? QueuedItems[0] : null;
    }

    private void UpdateFinishTime()
    {
        var (secondsUntilStopOrEnd, hasStop) = CalculateDurationUntilStopOrEnd();
        var currentTrackSeconds = _currentTrackRemainingMs / 1000.0;
        var totalSeconds = secondsUntilStopOrEnd + currentTrackSeconds;

        if (totalSeconds > 0)
        {
            var finishTime = DateTime.Now.AddSeconds(totalSeconds);
            QueueFinishTime = hasStop
                ? $"next stop at: {finishTime:HH:mm}"
                : $"queue finishes at: {finishTime:HH:mm}";
        }
        else
        {
            QueueFinishTime = "queue finishes at: --:--";
        }

        OnPropertyChanged(nameof(StatusText));
    }

    /// <summary>
    /// Calculates total duration until the first stop marker, or until the end if no stop marker exists.
    /// </summary>
    /// <returns>A tuple with (total seconds, whether a stop marker was found)</returns>
    private (double seconds, bool hasStop) CalculateDurationUntilStopOrEnd()
    {
        double totalSeconds = 0;

        foreach (var item in QueuedItems)
        {
            if (item is StopMarker)
            {
                return (totalSeconds, true);
            }

            // MessageMarker without delay behaves like a stop
            if (item is MessageMarker { HasDelay: false })
            {
                return (totalSeconds, true);
            }

            totalSeconds += item.Duration.TotalSeconds;
        }

        return (totalSeconds, false);
    }

    /// <summary>
    /// Updates the remaining time on the currently playing track
    /// </summary>
    public void UpdateCurrentTrackRemaining(long remainingMs)
    {
        _currentTrackRemainingMs = remainingMs;
        UpdateFinishTime();
    }

    [RelayCommand]
    private void Settings()
    {
        _commandBus?.SendAsync(new ShowSettingsCommand());
    }

    [RelayCommand]
    private void ToggleHistoryMode()
    {
        IsHistoryMode = !IsHistoryMode;
        _eventAggregator?.Publish(new HistoryModeChangedEvent(IsHistoryMode));
    }

    [RelayCommand]
    private void Shuffle()
    {
        if (_trackStore == null)
        {
            NotificationService?.ShowNotification("Track store not available", NotificationSeverity.Warning);
            return;
        }

        var randomTrack = _trackStore.GetRandomTrack();
        if (randomTrack != null)
        {
            AddTrack(randomTrack);
        }
        else
        {
            NotificationService?.ShowNotification("No tracks available", NotificationSeverity.Warning);
        }
    }

    /// <summary>
    /// Adds a stop marker to the queue. Can be called externally.
    /// </summary>
    public void AddStopMarker()
    {
        // Remove any auto-queued items when manually adding
        RemoveAutoQueuedItems();

        if (QueuedItems.Count >= MaxQueueItems)
        {
            NotificationService?.ShowNotification($"Queue is full (max {MaxQueueItems} items)", NotificationSeverity.Warning);
            return;
        }

        var wasEmpty = QueuedItems.Count == 0;
        QueuedItems.Add(new StopMarker());
        UpdateFinishTime();
        _logger?.Debug("Stop marker added to queue");

        if (wasEmpty)
        {
            _eventAggregator?.Publish(new QueueFirstItemChangedEvent(PeekFirst()));
        }
    }

    /// <summary>
    /// Adds a delay marker to the queue. Can be called externally.
    /// </summary>
    public void AddDelayMarker()
    {
        // Remove any auto-queued items when manually adding
        RemoveAutoQueuedItems();

        if (QueuedItems.Count >= MaxQueueItems)
        {
            NotificationService?.ShowNotification($"Queue is full (max {MaxQueueItems} items)", NotificationSeverity.Warning);
            return;
        }

        var wasEmpty = QueuedItems.Count == 0;
        QueuedItems.Add(new DelayMarker(DelaySeconds));
        UpdateFinishTime();
        _logger?.Debug($"Delay marker added to queue: {DelaySeconds} seconds");

        if (wasEmpty)
        {
            _eventAggregator?.Publish(new QueueFirstItemChangedEvent(PeekFirst()));
        }
    }

    /// <summary>
    /// Adds a message marker to the queue. Can be called externally.
    /// </summary>
    public void AddMessageMarker(string message, int? delaySeconds)
    {
        // Remove any auto-queued items when manually adding
        RemoveAutoQueuedItems();

        if (QueuedItems.Count >= MaxQueueItems)
        {
            NotificationService?.ShowNotification($"Queue is full (max {MaxQueueItems} items)", NotificationSeverity.Warning);
            return;
        }

        var wasEmpty = QueuedItems.Count == 0;
        var marker = new MessageMarker(message, delaySeconds);
        QueuedItems.Add(marker);
        UpdateFinishTime();

        var behavior = marker.HasDelay ? $"delay {delaySeconds}s" : "stop";
        _logger?.Debug($"Message marker added to queue: \"{message}\" ({behavior})");

        if (wasEmpty)
        {
            _eventAggregator?.Publish(new QueueFirstItemChangedEvent(PeekFirst()));
        }
    }

    public void UpdateDelaySeconds(int seconds)
    {
        DelaySeconds = Math.Clamp(seconds, 1, 300);
    }

    [RelayCommand]
    private void RequestStop()
    {
        AddStopMarker();
    }

    [RelayCommand]
    private void RequestDelay()
    {
        AddDelayMarker();
    }

    [RelayCommand]
    public void RemoveSelectedItem()
    {
        if (SelectedItem != null)
        {
            var item = SelectedItem;
            QueuedItems.Remove(item);
            SelectedItem = null;
            UpdateFinishTime();

            switch (item)
            {
                case Track track:
                    _logger?.Debug($"Track removed from queue: {track.Artist} - {track.Title}");
                    break;
                case StopMarker:
                    _logger?.Debug("Stop marker removed from queue");
                    break;
                case DelayMarker delay:
                    _logger?.Debug($"Delay marker removed from queue: {delay.Duration.TotalSeconds} seconds");
                    break;
            }
        }
    }

    [RelayCommand]
    public void ClearQueue()
    {
        if (QueuedItems.Count == 0) return;

        var count = QueuedItems.Count;
        QueuedItems.Clear();
        SelectedItem = null;
        UpdateFinishTime();
        _logger?.Debug($"Queue cleared: {count} items removed");

        // Publish events
        _eventAggregator?.Publish(new QueueClearedEvent());
        _eventAggregator?.Publish(new QueueChangedEvent(0, false, false));
        _eventAggregator?.Publish(new QueueFirstItemChangedEvent(null));
    }
}
