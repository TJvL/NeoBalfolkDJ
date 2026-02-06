using Avalonia.Controls;
using Avalonia.Input;
using NeoBalfolkDJ.ViewModels;

namespace NeoBalfolkDJ.Views;

public partial class NotificationView : UserControl
{
    public NotificationView()
    {
        InitializeComponent();
    }

    private async void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is NotificationViewModel viewModel)
        {
            await viewModel.CloseNotification();
        }
    }
}
