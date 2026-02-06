using System;
using Avalonia.Controls;
using NeoBalfolkDJ.ViewModels;

namespace NeoBalfolkDJ.Views;

public partial class ToolbarView : UserControl
{
    public ToolbarView()
    {
        InitializeComponent();
        
        DataContextChanged += (_, _) =>
        {
            if (DataContext is ToolbarViewModel viewModel)
            {
                viewModel.ClearQueueConfirmationRequested += OnClearQueueConfirmationRequested;
                viewModel.ClearHistoryConfirmationRequested += OnClearHistoryConfirmationRequested;
                viewModel.ExitConfirmationRequested += OnExitConfirmationRequested;
                viewModel.MessageMarkerRequested += OnMessageMarkerRequested;
            }
        };
    }
    
    private async void OnMessageMarkerRequested(object? sender, EventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;
        
        var dialog = new RequestMessageDialog();
        await dialog.ShowDialog((Window)topLevel);
        
        if (dialog.IsConfirmed && DataContext is ToolbarViewModel viewModel)
        {
            viewModel.ConfirmAddMessageMarker(dialog.Message, dialog.DelaySeconds);
        }
    }
    
    private async void OnClearQueueConfirmationRequested(object? sender, EventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;
        
        var dialog = new ConfirmationDialog();
        dialog.Setup("Clear Queue", "Are you sure you want to clear the entire queue?");
        
        await dialog.ShowDialog((Window)topLevel);
        
        if (dialog.IsConfirmed && DataContext is ToolbarViewModel viewModel)
        {
            viewModel.ConfirmClearQueue();
        }
    }
    
    private async void OnClearHistoryConfirmationRequested(object? sender, EventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;
        
        var dialog = new ConfirmationDialog();
        dialog.Setup("Clear History", "Are you sure you want to clear all history?");
        
        await dialog.ShowDialog((Window)topLevel);
        
        if (dialog.IsConfirmed && DataContext is ToolbarViewModel viewModel)
        {
            viewModel.ConfirmClearHistory();
        }
    }
    
    private async void OnExitConfirmationRequested(object? sender, EventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;
        
        var dialog = new ConfirmationDialog();
        dialog.Setup("Exit Application", "Are you sure you want to exit the application?");
        
        await dialog.ShowDialog((Window)topLevel);
        
        if (dialog.IsConfirmed && DataContext is ToolbarViewModel viewModel)
        {
            viewModel.ConfirmExit();
        }
    }
}
