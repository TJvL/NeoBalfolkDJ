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
        commandBus.SendAsync(new ClearQueueCommand());
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
        commandBus.SendAsync(new ClearHistoryCommand());
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
        commandBus.SendAsync(new AddMessageMarkerCommand(message, delaySeconds));
    }
}
