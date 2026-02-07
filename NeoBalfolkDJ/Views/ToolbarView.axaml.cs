using System;
using Avalonia.Controls;
using NeoBalfolkDJ.Helpers;
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

    private void OnMessageMarkerRequested(object? sender, EventArgs e)
    {
        AsyncHelper.SafeFireAndForget(async () =>
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return;

            var dialog = new RequestMessageDialog();
            await dialog.ShowDialog((Window)topLevel);

            if (dialog.IsConfirmed && DataContext is ToolbarViewModel viewModel)
            {
                viewModel.ConfirmAddMessageMarker(dialog.Message, dialog.DelaySeconds);
            }
        });
    }

    private void OnClearQueueConfirmationRequested(object? sender, EventArgs e)
    {
        AsyncHelper.SafeFireAndForget(async () =>
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return;

            var dialog = new ConfirmationDialog();
            dialog.Setup("Clear Queue", "Are you sure you want to clear the entire queue?");

            await dialog.ShowDialog((Window)topLevel);

            if (DataContext is ToolbarViewModel viewModel)
            {
                if (dialog.IsConfirmed)
                {
                    viewModel.ConfirmClearQueue();
                }
                else
                {
                    viewModel.CancelClearQueue();
                }
            }
        });
    }

    private void OnClearHistoryConfirmationRequested(object? sender, EventArgs e)
    {
        AsyncHelper.SafeFireAndForget(async () =>
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return;

            var dialog = new ConfirmationDialog();
            dialog.Setup("Clear History", "Are you sure you want to clear all history?");

            await dialog.ShowDialog((Window)topLevel);

            if (DataContext is ToolbarViewModel viewModel)
            {
                if (dialog.IsConfirmed)
                {
                    viewModel.ConfirmClearHistory();
                }
                else
                {
                    viewModel.CancelClearHistory();
                }
            }
        });
    }

    private void OnExitConfirmationRequested(object? sender, EventArgs e)
    {
        AsyncHelper.SafeFireAndForget(async () =>
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return;

            var dialog = new ConfirmationDialog();
            dialog.Setup("Exit Application", "Are you sure you want to exit the application?");

            await dialog.ShowDialog((Window)topLevel);

            if (DataContext is ToolbarViewModel viewModel)
            {
                if (dialog.IsConfirmed)
                {
                    viewModel.ConfirmExit();
                }
                else
                {
                    viewModel.CancelExit();
                }
            }
        });
    }
}
