using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using NeoBalfolkDJ.Helpers;
using NeoBalfolkDJ.Messaging;
using NeoBalfolkDJ.Messaging.Commands;
using NeoBalfolkDJ.Messaging.Events;
using NeoBalfolkDJ.Models;
using NeoBalfolkDJ.Services;

namespace NeoBalfolkDJ.ViewModels;

public sealed partial class MainWindowViewModel : ViewModelBase, IDisposable
{
    private readonly ILoggingService _logger;
    private readonly ICommandBus _commandBus;
    private readonly IEventAggregator _eventAggregator;
    private readonly List<IDisposable> _subscriptions = [];

    private ITrackStoreService TrackStore { get; }
    private ITrackPreloadService PreloadService { get; }
    private ISessionTrackHistoryService SessionHistory { get; }
    public TrackListViewModel TrackList { get; }
    public SettingsViewModel Settings { get; }
    public PlaybackViewModel Playback { get; }
    public QueueViewModel Queue { get; }
    public ToolbarViewModel Toolbar { get; }
    public NotificationViewModel Notification { get; }
    public HelpViewModel Help { get; }
    private INotificationService NotificationService { get; }
    private IDanceCategoryService DanceCategoryService { get; }
    private IDanceSynonymService DanceSynonymService { get; }
    private IWeightedRandomService WeightedRandomService { get; }

    private IPresentationDisplayService? _presentationService;
    private bool _autoQueueEnabled;
    private bool _allowDuplicates;
    private bool _disposed;

    /// <summary>
    /// Event raised when exit is requested
    /// </summary>
    public event EventHandler? ExitRequested;

    /// <summary>
    /// Event raised when history export is requested (MainWindow handles the file dialog)
    /// </summary>
    public event EventHandler? ExportHistoryRequested;

    [ObservableProperty]
    private ViewModelBase _currentView;

    [ObservableProperty]
    private bool _isSettingsVisible;

    [ObservableProperty]
    private bool _isHelpVisible;

    /// <summary>
    /// Design-time constructor
    /// </summary>
    public MainWindowViewModel() : this(null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!)
    {
        if (Design.IsDesignMode)
        {
            // Design-time initialization handled by child ViewModels
        }
    }

    public MainWindowViewModel(
        ILoggingService logger,
        ISettingsService settingsService,
        ITrackStoreService trackStore,
        ITrackPreloadService preloadService,
        ISessionTrackHistoryService sessionHistory,
        INotificationService notificationService,
        IDanceCategoryService danceCategoryService,
        IDanceSynonymService danceSynonymService,
        IWeightedRandomService weightedRandomService,
        ICommandBus commandBus,
        IEventAggregator eventAggregator,
        TrackListViewModel trackList,
        SettingsViewModel settings,
        PlaybackViewModel playback,
        QueueViewModel queue,
        ToolbarViewModel toolbar,
        NotificationViewModel notification,
        HelpViewModel help)
    {
        _ = settingsService; // Injected by DI, used by child ViewModels
        _logger = logger;
        _commandBus = commandBus;
        _eventAggregator = eventAggregator;
        TrackStore = trackStore;
        PreloadService = preloadService;
        SessionHistory = sessionHistory;
        NotificationService = notificationService;
        DanceCategoryService = danceCategoryService;
        DanceSynonymService = danceSynonymService;
        WeightedRandomService = weightedRandomService;
        TrackList = trackList;
        Settings = settings;
        Playback = playback;
        Queue = queue;
        Toolbar = toolbar;
        Notification = notification;
        Help = help;

        _currentView = TrackList;
        _autoQueueEnabled = Settings.AutoQueueRandomTrack;
        _allowDuplicates = Settings.AllowDuplicateTracksInQueue;

        // Load synonyms
        DanceSynonymService.Load();

        // Wire up TrackStore to ViewModels
        TrackList.SetTrackStore(TrackStore);
        TrackList.SetDanceCategoryService(DanceCategoryService);
        Queue.SetSessionHistory(SessionHistory);

        // Wire up DanceSynonymService to Settings
        Settings.SetSynonymService(DanceSynonymService);

        // Wire up NotificationService to child view models
        Queue.NotificationService = NotificationService;

        // Register command handlers
        RegisterCommandHandlers();

        // Subscribe to events from the event aggregator
        SubscribeToEvents();

        // Wire up toolbar exit event (still needed for window close)
        Toolbar.ExitRequested += (_, _) => ExitRequested?.Invoke(this, EventArgs.Empty);
        Toolbar.ExportHistoryRequested += (_, _) => ExportHistoryRequested?.Invoke(this, EventArgs.Empty);

        // Wire up other events (settings-related, still needed)
        Settings.BackRequested += (_, _) => HideSettings();
        Help.BackRequested += (_, _) => HideHelp();
        Settings.MusicDirectoryChanged += OnMusicDirectoryChanged;
        Settings.MaxQueueItemsChanged += OnMaxQueueItemsChanged;
        Settings.DelaySecondsChanged += OnDelaySecondsChanged;
        Settings.PresentationDisplayCountChanged += OnPresentationDisplayCountChanged;
        Settings.AutoQueueRandomTrackChanged += OnAutoQueueRandomTrackChanged;
        Settings.AllowDuplicateTracksInQueueChanged += OnAllowDuplicatesChanged;
        Settings.ThemeChanged += OnThemeChanged;
        NotificationService.NotificationRequested += OnNotificationRequested;
        TrackStore.TracksReloaded += OnTracksReloaded;

        // Wire up playback orchestration
        Queue.QueuedItems.CollectionChanged += OnQueueCollectionChanged;
        Playback.PropertyChanged += OnPlaybackPropertyChanged;

        // Initialize playback button state based on queue
        Playback.QueueHasItems = Queue.HasItems;

        // Load tracks from configured directory on startup
        LoadTracksFromSettings();
    }

    private void RegisterCommandHandlers()
    {
        // Register all command handlers
        _subscriptions.Add(_commandBus.RegisterHandler<ShowSettingsCommand>(_ =>
        {
            ShowSettings();
            return Task.CompletedTask;
        }));

        _subscriptions.Add(_commandBus.RegisterHandler<ShowHelpCommand>(_ =>
        {
            ShowHelp();
            return Task.CompletedTask;
        }));

        _subscriptions.Add(_commandBus.RegisterHandler<AddTrackToQueueCommand>(cmd =>
        {
            Queue.AddTrack(cmd.Track);
            return Task.CompletedTask;
        }));

        _subscriptions.Add(_commandBus.RegisterHandler<AddStopMarkerCommand>(_ =>
        {
            Queue.AddStopMarker();
            return Task.CompletedTask;
        }));

        _subscriptions.Add(_commandBus.RegisterHandler<AddDelayMarkerCommand>(_ =>
        {
            Queue.AddDelayMarker();
            return Task.CompletedTask;
        }));

        _subscriptions.Add(_commandBus.RegisterHandler<AddMessageMarkerCommand>(cmd =>
        {
            Queue.AddMessageMarker(cmd.Message, cmd.DelaySeconds);
            return Task.CompletedTask;
        }));

        _subscriptions.Add(_commandBus.RegisterHandler<ClearQueueCommand>(_ =>
        {
            Queue.ClearQueue();
            return Task.CompletedTask;
        }));

        _subscriptions.Add(_commandBus.RegisterHandler<RemoveSelectedQueueItemCommand>(_ =>
        {
            Queue.RemoveSelectedItem();
            return Task.CompletedTask;
        }));

        _subscriptions.Add(_commandBus.RegisterHandler<ClearHistoryCommand>(_ =>
        {
            ClearHistory();
            return Task.CompletedTask;
        }));

        _subscriptions.Add(_commandBus.RegisterHandler<RefreshAutoQueueCommand>(_ =>
        {
            TryAutoQueueRandomTrack();
            return Task.CompletedTask;
        }));

        _subscriptions.Add(_commandBus.RegisterHandler<PlayNextTrackCommand>(_ =>
        {
            // Cancel any running delay
            _delayCancellation?.Cancel();
            return PlayNextFromQueue();
        }));
    }

    private void SubscribeToEvents()
    {
        // Subscribe to track events
        _subscriptions.Add(_eventAggregator.Subscribe<TrackStartedEvent>(evt =>
        {
            // Update presentation display
            _presentationService?.UpdateCurrentTrack(evt.Track);
        }));

        _subscriptions.Add(_eventAggregator.Subscribe<TrackFinishedEvent>(evt =>
        {
            // Track finished playing naturally - add to session history
            SessionHistory.AddPlayedTrack(evt.Track);
        }));

        _subscriptions.Add(_eventAggregator.Subscribe<TrackAddedToQueueEvent>(evt =>
        {
            // Preload the track if it's the first item
            if (Queue.PeekFirst() is Track firstTrack && firstTrack.Equals(evt.Track))
            {
                AsyncHelper.SafeFireAndForget(async () =>
                {
                    if (!PreloadService.IsCached(evt.Track))
                    {
                        await PreloadService.PreloadAsync(evt.Track);
                    }
                });
            }
        }));

        _subscriptions.Add(_eventAggregator.Subscribe<QueueClearedEvent>(_ =>
        {
            _presentationService?.Clear();
        }));

        _subscriptions.Add(_eventAggregator.Subscribe<HistoryModeChangedEvent>(evt =>
        {
            // Sync history mode state to toolbar
            Toolbar.IsHistoryMode = evt.IsHistoryMode;
        }));

        _subscriptions.Add(_eventAggregator.Subscribe<QueueFirstItemChangedEvent>(evt =>
        {
            // Handle first item changes - preload tracks, update presentation "up next"
            OnFirstItemChanged(evt.FirstItem);
        }));
    }

    private void OnAllowDuplicatesChanged(object? sender, bool allow)
    {
        _allowDuplicates = allow;
        Queue.UpdateAllowDuplicates(allow);
    }

    private static void OnThemeChanged(object? sender, AppTheme theme)
    {
        if (Avalonia.Application.Current is App app)
        {
            app.ApplyTheme(theme);
        }
    }

    private void OnAutoQueueRandomTrackChanged(object? sender, bool enabled)
    {
        _autoQueueEnabled = enabled;

        // If disabled, remove any auto-queued items
        if (!enabled)
        {
            Queue.RemoveAutoQueuedItems();
        }
        // If enabled and currently playing, try to auto-queue
        else if (Playback.IsPlaying && !Queue.HasManualItems)
        {
            TryAutoQueueRandomTrack();
        }
    }

    private void TryAutoQueueRandomTrack()
    {
        // Don't auto-queue if disabled, has manual items, or already has an auto-queued item
        if (!_autoQueueEnabled || Queue.HasManualItems || Queue.HasAutoQueuedItem)
            return;

        // Use weighted random selection from the dance tree
        Func<Track, bool>? excludeFilter = _allowDuplicates ? null : track => Queue.IsTrackDuplicate(track);
        var randomTrack = WeightedRandomService.SelectRandomTrack(excludeFilter);

        if (randomTrack != null)
        {
            Queue.AddAutoQueuedTrack(randomTrack);
        }
        // If null, WeightedRandomService already showed a warning notification
    }

    private void OnDelaySecondsChanged(object? sender, int seconds)
    {
        Queue.UpdateDelaySeconds(seconds);
    }

    /// <summary>
    /// Sets the presentation display service (called from MainWindow after it's loaded)
    /// </summary>
    public void SetPresentationService(PresentationDisplayService service)
    {
        _presentationService = service;

        // Initialize with current settings
        _presentationService.UpdateDisplayCount(Settings.PresentationDisplayCount);

        // Sync current playback state
        if (Playback.IsStopMarkerActive)
        {
            _presentationService.ShowStopMarker();
        }
        else if (Playback.CurrentTrack != null)
        {
            _presentationService.UpdateCurrentTrack(Playback.CurrentTrack);
        }

        // Sync progress and next item
        _presentationService.UpdateProgress(Playback.Progress, Playback.Duration);
        _presentationService.UpdateNextItem(Queue.PeekFirst());
    }

    private void OnPresentationDisplayCountChanged(object? sender, int count)
    {
        _presentationService?.UpdateDisplayCount(count);
    }

    private void OnPlaybackPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(Playback.Progress) or nameof(Playback.Duration))
        {
            var remaining = (long)(Playback.Duration - Playback.Progress);
            Queue.UpdateCurrentTrackRemaining(remaining);

            // Update presentation displays
            _presentationService?.UpdateProgress(Playback.Progress, Playback.Duration);
        }
    }

    private void OnQueueCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Update playback button states based on queue status
        Playback.QueueHasItems = Queue.HasItems;

        // Update next item on presentation displays
        _presentationService?.UpdateNextItem(Queue.PeekFirst());

        // Try to auto-queue if enabled, playing, and queue has no manual items
        // Use Dispatcher.Post to avoid modifying collection during CollectionChanged event
        if (Playback.IsPlaying && !Queue.HasManualItems)
        {
            Dispatcher.UIThread.Post(TryAutoQueueRandomTrack);
        }
    }

    private void OnFirstItemChanged(IQueueItem? item)
    {
        AsyncHelper.SafeFireAndForget(async () =>
        {
            // Extract the track from the item (could be Track or AutoQueuedTrack)
            var track = item switch
            {
                AutoQueuedTrack autoQueued => autoQueued.Track,
                Track t => t,
                _ => null
            };

            // Preload if it's a track
            if (track != null && !PreloadService.IsCached(track))
            {
                var success = await PreloadService.PreloadAsync(track);
                if (!success)
                {
                    _logger.Warning($"Failed to preload first track: {track.Artist} - {track.Title}");
                }
            }
        });
    }

    private System.Threading.CancellationTokenSource? _delayCancellation;

    private async Task PlayNextFromQueue()
    {
        var item = Queue.DequeueNext();

        // Extract the actual track from AutoQueuedTrack if needed
        var actualTrack = item switch
        {
            AutoQueuedTrack autoQueued => autoQueued.Track,
            Track track => track,
            _ => null
        };

        switch (item)
        {
            case null:
                // Queue is empty, stop playback and remove auto-queued items
                await Playback.ClearTrackAsync();
                PreloadService.Clear();
                Queue.RemoveAutoQueuedItems();
                Queue.SetCurrentlyPlayingTrack(null);
                _presentationService?.Clear();
                return;

            case StopMarker:
                // Stop marker reached - stop audio and show stop state, wait for user
                await Playback.StopPlaybackAsync();
                Playback.ShowStopMarker();
                PreloadService.Clear();
                Queue.RemoveAutoQueuedItems();
                Queue.SetCurrentlyPlayingTrack(null);
                _presentationService?.ShowStopMarker();
                _presentationService?.UpdateNextItem(Queue.PeekFirst());
                return;

            case DelayMarker delay:
                // Delay marker reached - stop audio, show delay state with countdown, then continue
                await Playback.StopPlaybackAsync();
                _presentationService?.ShowDelayMarker((long)delay.Duration.TotalMilliseconds);
                _presentationService?.UpdateNextItem(Queue.PeekFirst());
                await RunDelayAsync(delay);
                return;

            case MessageMarker message:
                // Message marker reached - stop audio, show message
                await Playback.StopPlaybackAsync();
                Queue.SetCurrentlyPlayingTrack(null);

                if (message.HasDelay)
                {
                    // Message with delay - show message during countdown, then continue
                    _presentationService?.ShowMessageMarker(message.Message, (long)message.DelayDuration!.Value.TotalMilliseconds);
                    _presentationService?.UpdateNextItem(Queue.PeekFirst());
                    await RunMessageDelayAsync(message);
                }
                else
                {
                    // Message without delay - show message and stop, wait for user
                    Playback.ShowMessageMarker(message.Message);
                    PreloadService.Clear();
                    Queue.RemoveAutoQueuedItems();
                    _presentationService?.ShowMessageMarker(message.Message, null);
                    _presentationService?.UpdateNextItem(Queue.PeekFirst());
                }
                return;

            case AutoQueuedTrack:
            case Track:
                if (actualTrack == null) return;

                try
                {
                    // Update currently playing track for duplicate checking
                    Queue.SetCurrentlyPlayingTrack(actualTrack);

                    // Play the track - PlaybackService handles all operations on background thread
                    await Playback.PlayTrackAsync(actualTrack);
                    PreloadService.PromoteNextToCurrent();
                    _presentationService?.UpdateCurrentTrack(actualTrack);
                    _presentationService?.UpdateNextItem(Queue.PeekFirst());

                    // Auto-queue a random track if enabled and no manual items in queue
                    TryAutoQueueRandomTrack();
                }
                catch (Exception ex)
                {
                    _logger.Error($"Failed to play track: {actualTrack.FilePath}", ex);
                    NotificationService.ShowNotification("Failed to load track", NotificationSeverity.Error);

                    // Clear currently playing since it failed
                    Queue.SetCurrentlyPlayingTrack(null);

                    // Skip to next track
                    await PlayNextFromQueue();
                    return;
                }

                // Preload the new first item in queue if it's a track
                var nextItem = Queue.PeekFirst();
                var nextTrack = nextItem switch
                {
                    AutoQueuedTrack autoQueued => autoQueued.Track,
                    Track t => t,
                    _ => null
                };

                if (nextTrack != null && !PreloadService.IsCached(nextTrack))
                {
                    var success = await PreloadService.PreloadAsync(nextTrack);
                    if (!success)
                    {
                        _logger.Warning($"Failed to preload next track: {nextTrack.Artist} - {nextTrack.Title}");
                    }
                }
                break;
        }
    }

    private async Task RunDelayAsync(DelayMarker delay)
    {
        if (_delayCancellation != null) await _delayCancellation.CancelAsync();
        _delayCancellation = new System.Threading.CancellationTokenSource();
        var token = _delayCancellation.Token;

        var totalMs = (long)delay.Duration.TotalMilliseconds;
        Playback.ShowDelayMarker(totalMs);

        try
        {
            var elapsed = 0L;
            const int intervalMs = 250;

            while (elapsed < totalMs && !token.IsCancellationRequested)
            {
                await Task.Delay(intervalMs, token);
                elapsed += intervalMs;
                Playback.UpdateDelayProgress(elapsed, totalMs);
                _presentationService?.UpdateProgress(elapsed, totalMs);
            }

            if (!token.IsCancellationRequested)
            {
                // Delay completed, continue to next item
                await PlayNextFromQueue();
            }
        }
        catch (TaskCanceledException)
        {
            // Delay was canceled (user pressed next), handled elsewhere
        }
    }

    private async Task RunMessageDelayAsync(MessageMarker message)
    {
        if (_delayCancellation != null) await _delayCancellation.CancelAsync();
        _delayCancellation = new System.Threading.CancellationTokenSource();
        var token = _delayCancellation.Token;

        var totalMs = (long)message.DelayDuration!.Value.TotalMilliseconds;
        Playback.ShowMessageMarker(message.Message, totalMs);

        try
        {
            var elapsed = 0L;
            const int intervalMs = 250;

            while (elapsed < totalMs && !token.IsCancellationRequested)
            {
                await Task.Delay(intervalMs, token);
                elapsed += intervalMs;
                Playback.UpdateDelayProgress(elapsed, totalMs);
                _presentationService?.UpdateProgress(elapsed, totalMs);
            }

            if (!token.IsCancellationRequested)
            {
                // Delay completed, continue to next item
                await PlayNextFromQueue();
            }
        }
        catch (TaskCanceledException)
        {
            // Delay was canceled (user pressed next), handled elsewhere
        }
    }

    private void OnNotificationRequested(object? sender, NotificationEventArgs e)
    {
        AsyncHelper.SafeFireAndForget(async () =>
        {
            await Notification.ShowNotification(e.Message, e.Severity);
        });
    }

    private void OnTracksReloaded(object? sender, EventArgs e)
    {
        var count = TrackStore.Tracks.Count;
        if (count > 0)
        {
            // Assign tracks to dances in the tree
            WeightedRandomService.AssignTracksToTree(TrackStore.Tracks);

            NotificationService.ShowNotification($"Loaded {count} tracks", NotificationSeverity.Information);
        }
        else if (!string.IsNullOrWhiteSpace(TrackStore.MusicDirectoryPath))
        {
            NotificationService.ShowNotification("No tracks found in music directory", NotificationSeverity.Warning);
        }
    }

    private void LoadTracksFromSettings()
    {
        if (!string.IsNullOrWhiteSpace(Settings.MusicDirectoryPath))
        {
            TrackStore.SetMusicDirectory(Settings.MusicDirectoryPath);
        }
    }

    private void OnMusicDirectoryChanged(object? sender, string newDirectory)
    {
        AsyncHelper.SafeFireAndForget(async () =>
        {
            // Cancel any running delay
            if (_delayCancellation != null) await _delayCancellation.CancelAsync();

            // Stop playback and clear current track
            await Playback.ClearTrackAsync();
            PreloadService.Clear();
            Queue.ClearQueue();
            Queue.RemoveAutoQueuedItems();
            Queue.SetCurrentlyPlayingTrack(null);
            SessionHistory.Clear();
            _presentationService?.Clear();

            // Load tracks from new directory
            TrackStore.SetMusicDirectory(newDirectory);
        });
    }

    private void OnMaxQueueItemsChanged(object? sender, int maxItems)
    {
        Queue.UpdateMaxQueueItems(maxItems);
    }


    private void ClearHistory()
    {
        SessionHistory.Clear();
        NotificationService.ShowNotification("History cleared", NotificationSeverity.Information);
    }

    /// <summary>
    /// Exports the session history to a JSON file
    /// </summary>
    public async Task ExportHistoryAsync(string filePath)
    {
        try
        {
            await SessionHistory.ExportToFileAsync(filePath);
            NotificationService.ShowNotification("History exported successfully", NotificationSeverity.Information);
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to export history", ex);
            NotificationService.ShowNotification("Failed to export history", NotificationSeverity.Error);
        }
    }

    private void ShowSettings()
    {
        CurrentView = Settings;
        IsSettingsVisible = true;
    }

    private void HideSettings()
    {
        CurrentView = TrackList;
        IsSettingsVisible = false;
    }

    private void ShowHelp()
    {
        CurrentView = Help;
        IsHelpVisible = true;
    }

    private void HideHelp()
    {
        CurrentView = TrackList;
        IsHelpVisible = false;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        // Dispose all command/event subscriptions
        foreach (var subscription in _subscriptions)
        {
            subscription.Dispose();
        }
        _subscriptions.Clear();

        // Dispose child ViewModels that implement IDisposable
        Playback.Dispose();

        // Dispose presentation service
        (_presentationService as IDisposable)?.Dispose();

        // Dispose delay cancellation token
        _delayCancellation?.Dispose();

        // Clear event handlers
        ExitRequested = null;
        ExportHistoryRequested = null;
    }
}

