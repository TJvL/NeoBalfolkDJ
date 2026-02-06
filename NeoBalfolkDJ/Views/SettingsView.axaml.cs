using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using NeoBalfolkDJ.Services;
using NeoBalfolkDJ.ViewModels;

namespace NeoBalfolkDJ.Views;

public partial class SettingsView : UserControl
{
    private SettingsViewModel? _subscribedViewModel;
    
    public SettingsView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }
    
    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        // Unsubscribe from previous view model
        if (_subscribedViewModel != null)
        {
            _subscribedViewModel.ImportSynonymsRequested -= OnImportSynonymsRequested;
            _subscribedViewModel.ExportSynonymsRequested -= OnExportSynonymsRequested;
            _subscribedViewModel.DeleteSynonymLineRequested -= OnDeleteSynonymLineRequested;
            _subscribedViewModel = null;
        }
        
        // Subscribe to new view model
        if (DataContext is SettingsViewModel viewModel)
        {
            viewModel.ImportSynonymsRequested += OnImportSynonymsRequested;
            viewModel.ExportSynonymsRequested += OnExportSynonymsRequested;
            viewModel.DeleteSynonymLineRequested += OnDeleteSynonymLineRequested;
            _subscribedViewModel = viewModel;
        }
    }
    
    private async void OnImportClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;
        
        // Show confirmation dialog first
        var confirmDialog = new ConfirmationDialog();
        confirmDialog.Setup("Import Dance Synonyms", 
            "Importing will permanently overwrite the currently loaded synonyms.\n\n" +
            "Make sure you have exported a backup if needed.\n\n" +
            "Do you want to continue?");
        
        await confirmDialog.ShowDialog((Window)topLevel);
        
        if (!confirmDialog.IsConfirmed) return;
        
        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Import Dance Synonyms",
            AllowMultiple = false,
            FileTypeFilter = new List<FilePickerFileType>
            {
                new("JSON Files") { Patterns = new[] { "*.json" } },
                new("All Files") { Patterns = new[] { "*" } }
            }
        });
        
        if (files.Count > 0 && DataContext is SettingsViewModel viewModel)
        {
            viewModel.PerformImport(files[0].Path.LocalPath);
        }
    }
    
    private async void OnExportClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;
        
        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export Dance Synonyms",
            SuggestedFileName = "dancesynonyms.json",
            DefaultExtension = "json",
            FileTypeChoices = new List<FilePickerFileType>
            {
                new("JSON Files") { Patterns = new[] { "*.json" } }
            }
        });
        
        if (file != null && DataContext is SettingsViewModel viewModel)
        {
            viewModel.PerformExport(file.Path.LocalPath);
        }
    }
    
    private async void OnImportSynonymsRequested(object? sender, EventArgs e)
    {
        // This is called from ViewModel - redirect to click handler
        OnImportClick(sender, new RoutedEventArgs());
    }
    
    private async void OnExportSynonymsRequested(object? sender, EventArgs e)
    {
        // This is called from ViewModel - redirect to click handler
        OnExportClick(sender, new RoutedEventArgs());
    }
    
    private async void OnDeleteSynonymLineRequested(object? sender, DanceSynonymEntryViewModel entry)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;
        
        var dialog = new ConfirmationDialog();
        dialog.Setup("Delete Dance", 
            $"Are you sure you want to delete '{entry.Name}' and all its synonyms?");
        
        await dialog.ShowDialog((Window)topLevel);
        
        if (dialog.IsConfirmed && DataContext is SettingsViewModel viewModel)
        {
            viewModel.ConfirmDeleteSynonymLine(entry);
        }
    }
    
    private void OnNewSynonymKeyDown(object? sender, KeyEventArgs e)
    {
        if (sender is TextBox textBox && textBox.DataContext is DanceSynonymEntryViewModel entry)
        {
            if (e.Key == Key.Enter)
            {
                entry.ConfirmAddSynonymCommand.Execute(null);
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                entry.CancelAddSynonymCommand.Execute(null);
                e.Handled = true;
            }
        }
    }
    
    private async void OnBrowseButtonClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;
        
        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select Music Directory",
            AllowMultiple = false
        });
        
        if (folders.Count > 0)
        {
            var folder = folders.First();
            var path = folder.Path.LocalPath;
            
            if (DataContext is SettingsViewModel vm)
            {
                vm.MusicDirectoryPath = path;
            }
        }
    }
}
