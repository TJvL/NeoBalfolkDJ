using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using NeoBalfolkDJ.Helpers;
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

    private void OnImportDanceTreeRequested(object? sender, System.EventArgs e)
    {
        AsyncHelper.SafeFireAndForget(async () =>
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
                    new("JSON Files") { Patterns = ["*.json"] },
                    new("All Files") { Patterns = ["*"] }
                }
            });

            if (files.Count > 0 && DataContext is TrackListViewModel viewModel)
            {
                var filePath = files[0].Path.LocalPath;
                viewModel.ImportDanceTree(filePath);
            }
        }, userFriendlyError: "Failed to import dance tree");
    }

    private void OnExportDanceTreeRequested(object? sender, System.EventArgs e)
    {
        AsyncHelper.SafeFireAndForget(async () =>
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return;

            var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Export Dance Tree",
                // ReSharper disable once StringLiteralTypo
                SuggestedFileName = "dancetree.json",
                DefaultExtension = "json",
                FileTypeChoices = new List<FilePickerFileType>
                {
                    new("JSON Files") { Patterns = ["*.json"] }
                }
            });

            if (file != null && DataContext is TrackListViewModel viewModel)
            {
                var filePath = file.Path.LocalPath;
                viewModel.ExportDanceTree(filePath);
            }
        }, userFriendlyError: "Failed to export dance tree");
    }

    private void OnAddCategoryRequested(object? sender, System.EventArgs e)
    {
        AsyncHelper.SafeFireAndForget(async () =>
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return;

            var dialog = new AddDanceItemDialog();
            dialog.SetupForCategory();

            await dialog.ShowDialog((Window)topLevel);

            if (dialog is { IsConfirmed: true, ResultName: not null } && DataContext is TrackListViewModel viewModel)
            {
                viewModel.AddCategory(dialog.ResultName, dialog.ResultWeight);
            }
        });
    }

    private void OnAddDanceRequested(object? sender, System.EventArgs e)
    {
        AsyncHelper.SafeFireAndForget(async () =>
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return;

            var dialog = new AddDanceItemDialog();
            dialog.SetupForDance();

            await dialog.ShowDialog((Window)topLevel);

            if (dialog is { IsConfirmed: true, ResultName: not null } && DataContext is TrackListViewModel viewModel)
            {
                viewModel.AddDance(dialog.ResultName, dialog.ResultWeight);
            }
        });
    }

    private void OnDeleteItemRequested(object? sender, (object Item, DanceCategoryNode Parent) args)
    {
        AsyncHelper.SafeFireAndForget(async () =>
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
        });
    }

    private void OnImportDanceTreeClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is TrackListViewModel viewModel)
        {
            viewModel.RequestImportDanceTreeCommand.Execute(null);
        }
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
        if (sender is DataGrid { SelectedItem: Track track } &&
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
