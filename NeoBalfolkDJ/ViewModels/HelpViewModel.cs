using System;
using System.IO;
using System.Reflection;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeoBalfolkDJ.Services;

namespace NeoBalfolkDJ.ViewModels;

public partial class HelpViewModel : ViewModelBase
{
    private readonly ILoggingService _logger;

    [ObservableProperty]
    private string _markdownContent = string.Empty;

    /// <summary>
    /// Event raised when the user wants to go back to the main view
    /// </summary>
    public event EventHandler? BackRequested;

    /// <summary>
    /// Design-time constructor
    /// </summary>
    public HelpViewModel() : this(null!)
    {
        if (Design.IsDesignMode)
        {
            MarkdownContent = "# Sample Help\n\nThis is sample help content for design time.";
        }
    }

    public HelpViewModel(ILoggingService logger)
    {
        _logger = logger;

        if (!Design.IsDesignMode)
        {
            LoadMarkdownContent();
        }
    }

    private void LoadMarkdownContent()
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "NeoBalfolkDJ.Documentation.help.md";

            // Log all available resources for debugging
            var availableResources = assembly.GetManifestResourceNames();
            _logger.Info($"Available embedded resources: {string.Join(", ", availableResources)}");

            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream != null)
            {
                using var reader = new StreamReader(stream);
                MarkdownContent = reader.ReadToEnd();
                _logger.Info("Help documentation loaded successfully");
            }
            else
            {
                _logger.Warning($"Could not find embedded resource: {resourceName}");
                MarkdownContent = "# Help\n\nDocumentation could not be loaded.";
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to load help documentation: {ex.Message}");
            MarkdownContent = "# Help\n\nFailed to load documentation.";
        }
    }

    [RelayCommand]
    private void Back()
    {
        BackRequested?.Invoke(this, EventArgs.Empty);
    }
}


