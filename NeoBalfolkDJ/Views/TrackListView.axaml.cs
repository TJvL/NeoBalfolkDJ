using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;
using NeoBalfolkDJ.Models;
using NeoBalfolkDJ.ViewModels;

namespace NeoBalfolkDJ.Views;

public partial class TrackListView : UserControl
{
    private string? _lastSortColumn;
    private int _clickCount;

    public TrackListView()
    {
        InitializeComponent();
        
        // Subscribe to ViewModel events when DataContext changes
        DataContextChanged += (_, _) =>
        {
            if (DataContext is TrackListViewModel viewModel)
            {
                viewModel.ImportDanceTreeRequested += OnImportDanceTreeRequested;
                viewModel.ExportDanceTreeRequested += OnExportDanceTreeRequested;
                viewModel.AddCategoryRequested += OnAddCategoryRequested;
                viewModel.AddDanceRequested += OnAddDanceRequested;
                viewModel.DeleteItemRequested += OnDeleteItemRequested;
            }
        };
    }
    
    private async void OnImportDanceTreeRequested(object? sender, System.EventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;
        
        // Show confirmation dialog first
        var confirmDialog = new ConfirmationDialog();
        confirmDialog.Setup("Import Dance Tree", 
            "Importing will permanently overwrite the currently loaded dance tree.\n\n" +
            "Make sure you have exported a backup if needed.\n\n" +
            "Do you want to continue?");
        
        await confirmDialog.ShowDialog((Window)topLevel);
        
        if (!confirmDialog.IsConfirmed) return;
        
        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Import Dance Tree",
            AllowMultiple = false,
            FileTypeFilter = new List<FilePickerFileType>
            {
                new("JSON Files") { Patterns = new[] { "*.json" } },
                new("All Files") { Patterns = new[] { "*" } }
            }
        });
        
        if (files.Count > 0 && DataContext is TrackListViewModel viewModel)
        {
            var filePath = files[0].Path.LocalPath;
            viewModel.ImportDanceTree(filePath);
        }
    }
    
    private async void OnExportDanceTreeRequested(object? sender, System.EventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;
        
        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export Dance Tree",
            SuggestedFileName = "dancetree.json",
            DefaultExtension = "json",
            FileTypeChoices = new List<FilePickerFileType>
            {
                new("JSON Files") { Patterns = new[] { "*.json" } }
            }
        });
        
        if (file != null && DataContext is TrackListViewModel viewModel)
        {
            var filePath = file.Path.LocalPath;
            viewModel.ExportDanceTree(filePath);
        }
    }
    
    private async void OnAddCategoryRequested(object? sender, System.EventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;
        
        var dialog = new AddDanceItemDialog();
        dialog.SetupForCategory();
        
        await dialog.ShowDialog((Window)topLevel);
        
        if (dialog.IsConfirmed && dialog.ResultName != null && DataContext is TrackListViewModel viewModel)
        {
            viewModel.AddCategory(dialog.ResultName, dialog.ResultWeight);
        }
    }
    
    private async void OnAddDanceRequested(object? sender, System.EventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;
        
        var dialog = new AddDanceItemDialog();
        dialog.SetupForDance();
        
        await dialog.ShowDialog((Window)topLevel);
        
        if (dialog.IsConfirmed && dialog.ResultName != null && DataContext is TrackListViewModel viewModel)
        {
            viewModel.AddDance(dialog.ResultName, dialog.ResultWeight);
        }
    }
    
    private async void OnDeleteItemRequested(object? sender, (object Item, DanceCategoryNode Parent) args)
    {
        if (DataContext is not TrackListViewModel viewModel)
            return;
            
        // Check if confirmation is required
        if (viewModel.RequiresDeleteConfirmation(args.Item))
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return;
            
            var dialog = new ConfirmDeleteDialog();
            dialog.SetMessage(viewModel.GetDeleteDescription(args.Item));
            
            await dialog.ShowDialog((Window)topLevel);
            
            if (!dialog.IsConfirmed)
                return;
        }
        
        viewModel.DeleteItem(args.Item, args.Parent);
    }
    
    private void OnAddCategoryClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is TrackListViewModel viewModel)
        {
            viewModel.RequestAddCategoryCommand.Execute(null);
        }
    }
    
    private void OnAddDanceClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is TrackListViewModel viewModel)
        {
            viewModel.RequestAddDanceCommand.Execute(null);
        }
    }
    
    private void OnDeleteClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is TrackListViewModel viewModel)
        {
            viewModel.RequestDeleteItemCommand.Execute(null);
        }
    }

    private void DataGrid_DoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is DataGrid dataGrid && 
            dataGrid.SelectedItem is Track track &&
            DataContext is TrackListViewModel viewModel)
        {
            viewModel.OnTrackDoubleClicked(track);
        }
    }

    private void DataGrid_Sorting(object? sender, DataGridColumnEventArgs e)
    {
        if (sender is not DataGrid dataGrid)
            return;

        var column = e.Column;
        var columnHeader = column.Header?.ToString();

        // If clicking a different column, reset click count
        if (columnHeader != _lastSortColumn)
        {
            _lastSortColumn = columnHeader;
            _clickCount = 1;
            return; // Let default sorting happen (ascending)
        }

        // Same column clicked
        _clickCount++;

        // Third click - clear sorting
        if (_clickCount >= 3)
        {
            e.Handled = true;
            _clickCount = 0;
            _lastSortColumn = null;
            
            // Clear sort descriptions
            dataGrid.CollectionView?.SortDescriptions.Clear();
        }
        // Second click - let default sorting happen (descending)
    }
}
