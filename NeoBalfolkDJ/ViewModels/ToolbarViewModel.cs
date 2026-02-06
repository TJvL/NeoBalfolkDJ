using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeoBalfolkDJ.Models;
using NeoBalfolkDJ.Services;

namespace NeoBalfolkDJ.ViewModels;

public partial class ToolbarViewModel : ViewModelBase
{
    private TrackStoreService? _trackStore;

    public NotificationService? NotificationService { get; set; }

    [ObservableProperty]
    private bool _isHistoryMode;

    /// <summary>
    /// Event raised when settings is requested
    /// </summary>
    public event EventHandler? SettingsRequested;

    /// <summary>
    /// Event raised when help is requested
    /// </summary>
    public event EventHandler? HelpRequested;

    /// <summary>
    /// Event raised when exit is requested
    /// </summary>
    public event EventHandler? ExitRequested;

    /// <summary>
    /// Event raised when a track should be added to queue
    /// </summary>
#pragma warning disable CS0067 // Event is never used - kept for future use
    public event EventHandler<Track>? TrackAddRequested;
#pragma warning restore CS0067

    /// <summary>
    /// Event raised when a stop marker should be added to queue
    /// </summary>
    public event EventHandler? StopMarkerRequested;

    /// <summary>
    /// Event raised when a delay marker should be added to queue
    /// </summary>
    public event EventHandler? DelayMarkerRequested;

    /// <summary>
    /// Event raised when a message marker should be added to queue (View shows dialog)
    /// </summary>
    public event EventHandler? MessageMarkerRequested;

    /// <summary>
    /// Event raised when user confirms adding a message marker (with message and optional delay)
    /// </summary>
    public event EventHandler<(string message, int? delaySeconds)>? MessageMarkerAddRequested;

    /// <summary>
    /// Event raised when the selected item should be removed from queue
    /// </summary>
    public event EventHandler? RemoveSelectedRequested;

    /// <summary>
    /// Event raised when the queue should be cleared
    /// </summary>
    public event EventHandler? ClearQueueRequested;

    /// <summary>
    /// Event raised when history export is requested
    /// </summary>
    public event EventHandler? ExportHistoryRequested;

    /// <summary>
    /// Event raised when history should be cleared
    /// </summary>
    public event EventHandler? ClearHistoryRequested;

    /// <summary>
    /// Event raised when clear queue confirmation is needed (View shows dialog)
    /// </summary>
    public event EventHandler? ClearQueueConfirmationRequested;

    /// <summary>
    /// Event raised when clear history confirmation is needed (View shows dialog)
    /// </summary>
    public event EventHandler? ClearHistoryConfirmationRequested;

    /// <summary>
    /// Event raised when exit confirmation is needed (View shows dialog)
    /// </summary>
    public event EventHandler? ExitConfirmationRequested;

    /// <summary>
    /// Event raised when shuffle/random track is requested
    /// </summary>
    public event EventHandler? ShuffleRequested;

    public void SetTrackStore(TrackStoreService trackStore)
    {
        _trackStore = trackStore;
    }

    [RelayCommand]
    private void Settings()
    {
        SettingsRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void Help()
    {
        HelpRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void Shuffle()
    {
        ShuffleRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void RequestStop()
    {
        StopMarkerRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void RequestDelay()
    {
        DelayMarkerRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void RequestMessage()
    {
        MessageMarkerRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void RemoveSelectedItem()
    {
        RemoveSelectedRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void ClearQueue()
    {
        ClearQueueConfirmationRequested?.Invoke(this, EventArgs.Empty);
    }
    
    /// <summary>
    /// Called by View after user confirms clearing the queue.
    /// </summary>
    public void ConfirmClearQueue()
    {
        ClearQueueRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void ExportHistory()
    {
        ExportHistoryRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void ClearHistory()
    {
        ClearHistoryConfirmationRequested?.Invoke(this, EventArgs.Empty);
    }
    
    /// <summary>
    /// Called by View after user confirms clearing history.
    /// </summary>
    public void ConfirmClearHistory()
    {
        ClearHistoryRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void Exit()
    {
        ExitConfirmationRequested?.Invoke(this, EventArgs.Empty);
    }
    
    /// <summary>
    /// Called by View after user confirms exit.
    /// </summary>
    public void ConfirmExit()
    {
        ExitRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Called by View after user confirms adding a message marker.
    /// </summary>
    public void ConfirmAddMessageMarker(string message, int? delaySeconds)
    {
        MessageMarkerAddRequested?.Invoke(this, (message, delaySeconds));
    }
}
