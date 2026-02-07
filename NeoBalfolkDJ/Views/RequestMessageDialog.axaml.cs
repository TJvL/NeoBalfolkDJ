using Avalonia.Controls;
using Avalonia.Interactivity;

namespace NeoBalfolkDJ.Views;

public partial class RequestMessageDialog : Window
{
    public bool IsConfirmed { get; private set; }
    public string Message => MessageTextBox.Text ?? string.Empty;
    public int? DelaySeconds => UseDelayCheckBox.IsChecked == true
        ? (int)(DelaySecondsUpDown.Value ?? 30)
        : null;

    public RequestMessageDialog()
    {
        InitializeComponent();
    }

    private void OnMessageTextChanged(object? sender, TextChangedEventArgs e)
    {
        var length = MessageTextBox.Text?.Length ?? 0;
        CharacterCountText.Text = $"{length}/60";
        OkButton.IsEnabled = length > 0;
    }

    private void OnUseDelayChanged(object? sender, RoutedEventArgs e)
    {
        DelayPanel.IsVisible = UseDelayCheckBox.IsChecked == true;
    }

    private void OnOkClick(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(MessageTextBox.Text))
            return;

        IsConfirmed = true;
        Close();
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        IsConfirmed = false;
        Close();
    }
}

