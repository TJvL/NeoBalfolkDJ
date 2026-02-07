using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeoBalfolkDJ.Messaging;
using NeoBalfolkDJ.Messaging.Commands;

namespace NeoBalfolkDJ.ViewModels;

public partial class ToolbarViewModel(ICommandBus commandBus) : ViewModelBase
{
    [ObservableProperty]
    private bool _isHistoryMode;

    [ObservableProperty]
    private bool _canClearQueue;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ClearHistoryCommand))]
    [NotifyCanExecuteChangedFor(nameof(ExportHistoryCommand))]
    private bool _hasHistory;

    /// <summary>
    /// Event raised when exit is requested (after confirmation)
    /// </summary>
    public event EventHandler? ExitRequested;

    /// <summary>
    /// Event raised when history export is requested (View handles file dialog)
    /// </summary>
    public event EventHandler? ExportHistoryRequested;

    /// <summary>
    /// Event raised when a message marker should be added to queue (View shows dialog)
    /// </summary>
    public event EventHandler? MessageMarkerRequested;

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

    [RelayCommand]
    private void Settings()
    {
        commandBus.SendAsync(new ShowSettingsCommand());
    }

    [RelayCommand]
    private void Help()
    {
        commandBus.SendAsync(new ShowHelpCommand());
    }

    [RelayCommand]
    private void Shuffle()
    {
        commandBus.SendAsync(new RefreshAutoQueueCommand());
    }

    [RelayCommand]
    private void RequestStop()
    {
        commandBus.SendAsync(new AddStopMarkerCommand());
    }

    [RelayCommand]
    private void RequestDelay()
    {
        commandBus.SendAsync(new AddDelayMarkerCommand(30)); // Default delay from settings will be handled by handler
    }

    [RelayCommand]
    private void RequestMessage()
    {
        MessageMarkerRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void RemoveSelectedItem()
    {
        commandBus.SendAsync(new RemoveSelectedQueueItemCommand());
    }

    private bool _isClearQueueDialogOpen;
    
    [RelayCommand]
    private void ClearQueue()
    {
        // Guard against re-entry (e.g., Enter key triggering command again after dialog closes)
        if (_isClearQueueDialogOpen) return;
        _isClearQueueDialogOpen = true;
        ClearQueueConfirmationRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Called by View after user confirms clearing the queue.
    /// </summary>
    public void ConfirmClearQueue()
    {
        _isClearQueueDialogOpen = false;
        commandBus.SendAsync(new ClearQueueCommand());
    }
    
    /// <summary>
    /// Called by View when user cancels clear queue dialog.
    /// </summary>
    public void CancelClearQueue()
    {
        _isClearQueueDialogOpen = false;
    }

    [RelayCommand(CanExecute = nameof(HasHistory))]
    private void ExportHistory()
    {
        ExportHistoryRequested?.Invoke(this, EventArgs.Empty);
    }

    private bool _isClearHistoryDialogOpen;

    [RelayCommand(CanExecute = nameof(HasHistory))]
    private void ClearHistory()
    {
        // Guard against re-entry (e.g., Enter key triggering command again after dialog closes)
        if (_isClearHistoryDialogOpen) return;
        _isClearHistoryDialogOpen = true;
        ClearHistoryConfirmationRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Called by View after user confirms clearing history.
    /// </summary>
    public void ConfirmClearHistory()
    {
        _isClearHistoryDialogOpen = false;
        commandBus.SendAsync(new ClearHistoryCommand());
    }
    
    /// <summary>
    /// Called by View when user cancels clear history dialog.
    /// </summary>
    public void CancelClearHistory()
    {
        _isClearHistoryDialogOpen = false;
    }

    private bool _isExitDialogOpen;
    
    [RelayCommand]
    private void Exit()
    {
        // Guard against re-entry (e.g., Enter key triggering command again after dialog closes)
        if (_isExitDialogOpen) return;
        _isExitDialogOpen = true;
        ExitConfirmationRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Called by View after user confirms exit.
    /// </summary>
    public void ConfirmExit()
    {
        _isExitDialogOpen = false;
        ExitRequested?.Invoke(this, EventArgs.Empty);
    }
    
    /// <summary>
    /// Called by View when user cancels exit dialog.
    /// </summary>
    public void CancelExit()
    {
        _isExitDialogOpen = false;
    }

    /// <summary>
    /// Called by View after user confirms adding a message marker.
    /// </summary>
    public void ConfirmAddMessageMarker(string message, int? delaySeconds)
    {
        commandBus.SendAsync(new AddMessageMarkerCommand(message, delaySeconds));
    }
}
