using System;
using System.Collections.Specialized;
using System.ComponentModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using NeoBalfolkDJ.Models;
using NeoBalfolkDJ.Services;

namespace NeoBalfolkDJ.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public TrackStoreService TrackStore { get; } = new();
    public TrackPreloadService PreloadService { get; } = new();
    public SessionTrackHistoryService SessionHistory { get; } = new();
    public TrackListViewModel TrackList { get; } = new();
    public SettingsViewModel Settings { get; } = new();
    public PlaybackViewModel Playback { get; } = new();
    public QueueViewModel Queue { get; } = new();
    public ToolbarViewModel Toolbar { get; } = new();
    public NotificationViewModel Notification { get; } = new();
    public HelpViewModel Help { get; } = new();
    public NotificationService NotificationService { get; } = new();
    public DanceCategoryService DanceCategoryService { get; }
    public DanceSynonymService DanceSynonymService { get; }
    public WeightedRandomService WeightedRandomService { get; }
    
    private PresentationDisplayService? _presentationService;
    private bool _autoQueueEnabled;
    private bool _allowDuplicates;

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
    
    public MainWindowViewModel()
    {
        _currentView = TrackList;
        _autoQueueEnabled = Settings.AutoQueueRandomTrack;
        _allowDuplicates = Settings.AllowDuplicateTracksInQueue;
        
        // Initialize DanceCategoryService with NotificationService
        DanceCategoryService = new DanceCategoryService(NotificationService);
        
        // Initialize DanceSynonymService with NotificationService
        DanceSynonymService = new DanceSynonymService(NotificationService);
        DanceSynonymService.Load();
        
        // Initialize WeightedRandomService
        WeightedRandomService = new WeightedRandomService(DanceCategoryService, DanceSynonymService, NotificationService);
        
        // Wire up TrackStore to ViewModels
        TrackList.SetTrackStore(TrackStore);
        TrackList.SetDanceCategoryService(DanceCategoryService);
        Toolbar.SetTrackStore(TrackStore);
        Queue.SetSessionHistory(SessionHistory);
        
        // Wire up DanceSynonymService to Settings
        Settings.SetSynonymService(DanceSynonymService);
        
        // Wire up NotificationService to child view models
        Queue.NotificationService = NotificationService;
        Toolbar.NotificationService = NotificationService;
        
        // Wire up toolbar events
        Toolbar.SettingsRequested += (_, _) => ShowSettings();
        Toolbar.HelpRequested += (_, _) => ShowHelp();
        Toolbar.ExitRequested += (_, _) => ExitRequested?.Invoke(this, EventArgs.Empty);
        Toolbar.TrackAddRequested += (_, track) => Queue.AddTrack(track);
        Toolbar.ShuffleRequested += OnShuffleRequested;
        Toolbar.StopMarkerRequested += (_, _) => Queue.AddStopMarker();
        Toolbar.DelayMarkerRequested += (_, _) => Queue.AddDelayMarker();
        Toolbar.MessageMarkerAddRequested += (_, args) => Queue.AddMessageMarker(args.message, args.delaySeconds);
        Toolbar.RemoveSelectedRequested += (_, _) => Queue.RemoveSelectedItem();
        Toolbar.ClearQueueRequested += (_, _) => Queue.ClearQueue();
        Toolbar.ExportHistoryRequested += (_, _) => ExportHistoryRequested?.Invoke(this, EventArgs.Empty);
        Toolbar.ClearHistoryRequested += (_, _) => ClearHistory();
        
        // Wire up queue history mode to toolbar
        Queue.HistoryModeChanged += (_, isHistoryMode) => Toolbar.IsHistoryMode = isHistoryMode;
        
        // Wire up queue refresh auto-queued track event
        Queue.RefreshAutoQueuedTrackRequested += OnRefreshAutoQueuedTrackRequested;
        
        // Wire up other events
        Settings.BackRequested += (_, _) => HideSettings();
        Help.BackRequested += (_, _) => HideHelp();
        Settings.MusicDirectoryChanged += OnMusicDirectoryChanged;
        Settings.MaxQueueItemsChanged += OnMaxQueueItemsChanged;
        Settings.DelaySecondsChanged += OnDelaySecondsChanged;
        Settings.PresentationDisplayCountChanged += OnPresentationDisplayCountChanged;
        Settings.AutoQueueRandomTrackChanged += OnAutoQueueRandomTrackChanged;
        Settings.AllowDuplicateTracksInQueueChanged += OnAllowDuplicatesChanged;
        Settings.ThemeChanged += OnThemeChanged;
        TrackList.TrackDoubleClicked += OnTrackDoubleClicked;
        NotificationService.NotificationRequested += OnNotificationRequested;
        TrackStore.TracksReloaded += OnTracksReloaded;
        
        // Wire up playback orchestration
        Queue.QueuedItems.CollectionChanged += OnQueueCollectionChanged;
        Queue.FirstItemChanged += OnFirstItemChanged;
        Playback.PlaybackStartRequested += OnPlaybackStartRequested;
        Playback.NextTrackRequested += OnNextTrackRequested;
        Playback.TrackFinished += OnTrackFinished;
        Playback.TrackCleared += OnTrackCleared;
        Playback.PropertyChanged += OnPlaybackPropertyChanged;
        
        // Initialize playback button state based on queue
        Playback.QueueHasItems = Queue.HasItems;
        
        // Load tracks from configured directory on startup
        LoadTracksFromSettings();
    }

    private void OnTrackCleared(object? sender, EventArgs e)
    {
        // Track was cleared (user cleared or programmatically) - clear the currently playing track reference
        Queue.SetCurrentlyPlayingTrack(null);
    }

    private void OnTrackFinished(object? sender, EventArgs e)
    {
        // Track finished playing naturally - add to session history
        if (Playback.CurrentTrack != null)
        {
            SessionHistory.AddPlayedTrack(Playback.CurrentTrack);
        }
    }

    private void OnAllowDuplicatesChanged(object? sender, bool allow)
    {
        _allowDuplicates = allow;
        Queue.UpdateAllowDuplicates(allow);
    }

    private void OnThemeChanged(object? sender, AppTheme theme)
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

    private void OnRefreshAutoQueuedTrackRequested(object? sender, AutoQueuedTrack autoQueued)
    {
        // Create filter that excludes the current auto-queued track AND applies normal duplicate rules
        Func<Track, bool> excludeFilter = track =>
        {
            // Always exclude the current auto-queued track being replaced
            if (track.Equals(autoQueued.Track))
                return true;
            
            // Apply normal duplicate rules if duplicates are not allowed
            if (!_allowDuplicates && Queue.IsTrackDuplicate(track))
                return true;
            
            return false;
        };

        var randomTrack = WeightedRandomService.SelectRandomTrack(excludeFilter);
        
        if (randomTrack != null)
        {
            Queue.ReplaceAutoQueuedTrack(autoQueued, randomTrack);
        }
        else
        {
            NotificationService.ShowNotification("No other tracks available for random selection", NotificationSeverity.Warning);
        }
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
    
    private async void OnFirstItemChanged(object? sender, IQueueItem? item)
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
                LoggingService.Warning($"Failed to preload first track: {track.Artist} - {track.Title}");
            }
        }
    }
    
    private async void OnPlaybackStartRequested(object? sender, EventArgs e)
    {
        // User pressed play with no current track - start playing from queue
        await PlayNextFromQueue();
    }
    
    private async void OnNextTrackRequested(object? sender, EventArgs e)
    {
        // Cancel any running delay
        _delayCancellation?.Cancel();
        
        // Current track ended or user pressed next - play next from queue
        await PlayNextFromQueue();
    }
    
    private System.Threading.CancellationTokenSource? _delayCancellation;
    
    private async System.Threading.Tasks.Task PlayNextFromQueue()
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
                PreloadService.ClearAll();
                Queue.RemoveAutoQueuedItems();
                Queue.SetCurrentlyPlayingTrack(null);
                _presentationService?.Clear();
                return;
                
            case StopMarker:
                // Stop marker reached - stop audio and show stop state, wait for user
                await Playback.StopPlaybackAsync();
                Playback.ShowStopMarker();
                PreloadService.ClearAll();
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
                    PreloadService.ClearAll();
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
                    LoggingService.Error($"Failed to play track: {actualTrack.FilePath}", ex);
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
                        LoggingService.Warning($"Failed to preload next track: {nextTrack.Artist} - {nextTrack.Title}");
                    }
                }
                break;
        }
    }
    
    private async System.Threading.Tasks.Task RunDelayAsync(DelayMarker delay)
    {
        _delayCancellation?.Cancel();
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
                await System.Threading.Tasks.Task.Delay(intervalMs, token);
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
        catch (System.Threading.Tasks.TaskCanceledException)
        {
            // Delay was cancelled (user pressed next), handled elsewhere
        }
    }
    
    private async System.Threading.Tasks.Task RunMessageDelayAsync(MessageMarker message)
    {
        _delayCancellation?.Cancel();
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
                await System.Threading.Tasks.Task.Delay(intervalMs, token);
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
        catch (System.Threading.Tasks.TaskCanceledException)
        {
            // Delay was cancelled (user pressed next), handled elsewhere
        }
    }
    
    private async void OnNotificationRequested(object? sender, NotificationEventArgs e)
    {
        await Notification.ShowNotification(e.Message, e.Severity);
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
    
    private async void OnMusicDirectoryChanged(object? sender, string newDirectory)
    {
        // Cancel any running delay
        _delayCancellation?.Cancel();
        
        // Stop playback and clear current track
        await Playback.ClearTrackAsync();
        PreloadService.ClearAll();
        Queue.ClearQueue();
        Queue.RemoveAutoQueuedItems();
        Queue.SetCurrentlyPlayingTrack(null);
        SessionHistory.Clear();
        _presentationService?.Clear();
        
        // Load tracks from new directory
        TrackStore.SetMusicDirectory(newDirectory);
    }

    private void OnMaxQueueItemsChanged(object? sender, int maxItems)
    {
        Queue.UpdateMaxQueueItems(maxItems);
    }

    private void OnTrackDoubleClicked(object? sender, Track track)
    {
        Queue.AddTrack(track);
    }

    private void OnShuffleRequested(object? sender, EventArgs e)
    {
        // Get the selected branch from the dance tree (defaults to root)
        var selectedNode = TrackList.SelectedDanceNode;
        if (selectedNode == null)
        {
            NotificationService.ShowNotification("No dance category selected", NotificationSeverity.Warning);
            return;
        }
        
        // Use weighted random selection from the selected branch, respecting duplicate settings
        Func<Track, bool>? excludeFilter = _allowDuplicates ? null : track => Queue.IsTrackDuplicate(track);
        var randomTrack = WeightedRandomService.SelectRandomTrackFromBranch(selectedNode, excludeFilter);
        
        if (randomTrack != null)
        {
            Queue.AddTrack(randomTrack);
        }
        // If null, WeightedRandomService already showed a warning notification
    }

    private void ClearHistory()
    {
        SessionHistory.Clear();
        NotificationService.ShowNotification("History cleared", NotificationSeverity.Information);
    }

    /// <summary>
    /// Exports the session history to a JSON file
    /// </summary>
    public async System.Threading.Tasks.Task ExportHistoryAsync(string filePath)
    {
        try
        {
            await SessionHistory.ExportToJsonAsync(filePath);
            NotificationService.ShowNotification("History exported successfully", NotificationSeverity.Information);
        }
        catch (Exception ex)
        {
            LoggingService.Error("Failed to export history", ex);
            NotificationService.ShowNotification("Failed to export history", NotificationSeverity.Error);
        }
    }
    
    public void ShowSettings()
    {
        CurrentView = Settings;
        IsSettingsVisible = true;
    }
    
    public void HideSettings()
    {
        CurrentView = TrackList;
        IsSettingsVisible = false;
    }
    
    public void ShowHelp()
    {
        CurrentView = Help;
        IsHelpVisible = true;
    }
    
    public void HideHelp()
    {
        CurrentView = TrackList;
        IsHelpVisible = false;
    }
}

