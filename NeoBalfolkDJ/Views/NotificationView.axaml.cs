using Avalonia.Controls;
using Avalonia.Input;
using NeoBalfolkDJ.Helpers;
using NeoBalfolkDJ.ViewModels;

namespace NeoBalfolkDJ.Views;

public partial class NotificationView : UserControl
{
    public NotificationView()
    {
        InitializeComponent();
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        AsyncHelper.SafeFireAndForget(async () =>
        {
            if (DataContext is NotificationViewModel viewModel)
            {
                await viewModel.CloseNotification();
            }
        });
    }
}
