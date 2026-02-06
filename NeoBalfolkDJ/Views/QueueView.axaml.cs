using Avalonia.Controls;
using Avalonia.Interactivity;
using NeoBalfolkDJ.Models;
using NeoBalfolkDJ.ViewModels;

namespace NeoBalfolkDJ.Views;

public partial class QueueView : UserControl
{
    public QueueView()
    {
        InitializeComponent();
    }

    private void OnRefreshAutoQueuedClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button && 
            button.DataContext is AutoQueuedTrack autoQueued &&
            DataContext is QueueViewModel viewModel)
        {
            viewModel.RefreshAutoQueuedTrack(autoQueued);
        }
    }

    private void OnPinAutoQueuedClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button && 
            button.DataContext is AutoQueuedTrack autoQueued &&
            DataContext is QueueViewModel viewModel)
        {
            viewModel.PinAutoQueuedTrack(autoQueued);
        }
    }
}
