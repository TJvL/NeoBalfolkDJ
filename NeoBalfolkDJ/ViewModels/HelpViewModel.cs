using System;
using System.IO;
using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeoBalfolkDJ.Services;

namespace NeoBalfolkDJ.ViewModels;

public partial class HelpViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _markdownContent = string.Empty;

    /// <summary>
    /// Event raised when the user wants to go back to the main view
    /// </summary>
    public event EventHandler? BackRequested;

    public HelpViewModel()
    {
        LoadMarkdownContent();
    }

    private void LoadMarkdownContent()
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "NeoBalfolkDJ.Assets.main.md";
            
            // Log all available resources for debugging
            var availableResources = assembly.GetManifestResourceNames();
            LoggingService.Info($"Available embedded resources: {string.Join(", ", availableResources)}");
            
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream != null)
            {
                using var reader = new StreamReader(stream);
                MarkdownContent = reader.ReadToEnd();
                LoggingService.Info("Help documentation loaded successfully");
            }
            else
            {
                LoggingService.Warning($"Could not find embedded resource: {resourceName}");
                MarkdownContent = "# Help\n\nDocumentation could not be loaded.";
            }
        }
        catch (Exception ex)
        {
            LoggingService.Error($"Failed to load help documentation: {ex.Message}");
            MarkdownContent = "# Help\n\nFailed to load documentation.";
        }
    }

    [RelayCommand]
    private void Back()
    {
        BackRequested?.Invoke(this, EventArgs.Empty);
    }
}


