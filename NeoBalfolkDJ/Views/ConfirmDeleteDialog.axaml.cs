using Avalonia.Controls;
using Avalonia.Interactivity;

namespace NeoBalfolkDJ.Views;

public partial class ConfirmDeleteDialog : Window
{
    public bool IsConfirmed { get; private set; }

    public ConfirmDeleteDialog()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Sets the message to display in the dialog.
    /// </summary>
    public void SetMessage(string message)
    {
        MessageText.Text = message;
    }

    private void OnYesClick(object? sender, RoutedEventArgs e)
    {
        IsConfirmed = true;
        Close();
    }

    private void OnNoClick(object? sender, RoutedEventArgs e)
    {
        IsConfirmed = false;
        Close();
    }
}
