using Avalonia.Controls;
using Avalonia.Interactivity;

namespace NeoBalfolkDJ.Views;

public partial class ConfirmationDialog : Window
{
    public bool IsConfirmed { get; private set; }
    
    public ConfirmationDialog()
    {
        InitializeComponent();
    }
    
    /// <summary>
    /// Sets the dialog title and message.
    /// </summary>
    public void Setup(string title, string message)
    {
        Title = title;
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
