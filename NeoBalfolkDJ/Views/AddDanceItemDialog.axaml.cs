using Avalonia.Controls;
using Avalonia.Interactivity;

namespace NeoBalfolkDJ.Views;

public partial class AddDanceItemDialog : Window
{
    public string? ResultName { get; private set; }
    public int ResultWeight { get; private set; }
    public bool IsConfirmed { get; private set; }
    
    public AddDanceItemDialog()
    {
        InitializeComponent();
    }
    
    /// <summary>
    /// Sets up the dialog for adding a category.
    /// </summary>
    public void SetupForCategory()
    {
        Title = "Add Category";
        TitleText.Text = "Add Category";
    }
    
    /// <summary>
    /// Sets up the dialog for adding a dance.
    /// </summary>
    public void SetupForDance()
    {
        Title = "Add Dance";
        TitleText.Text = "Add Dance";
    }
    
    private void OnOkClick(object? sender, RoutedEventArgs e)
    {
        var name = NameTextBox.Text?.Trim();
        
        if (string.IsNullOrWhiteSpace(name))
        {
            // Focus on name field if empty
            NameTextBox.Focus();
            return;
        }
        
        ResultName = name;
        ResultWeight = (int)(WeightInput.Value ?? 1);
        IsConfirmed = true;
        Close();
    }
    
    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        IsConfirmed = false;
        Close();
    }
}
