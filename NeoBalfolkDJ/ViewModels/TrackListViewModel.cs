using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeoBalfolkDJ.Messaging;
using NeoBalfolkDJ.Messaging.Commands;
using NeoBalfolkDJ.Models;
using NeoBalfolkDJ.Services;

namespace NeoBalfolkDJ.ViewModels;

public partial class TrackListViewModel : ViewModelBase
{
    private readonly IDanceTreeHistoryService? _historyService;
    private readonly ICommandBus? _commandBus;

    private ITrackStoreService? _trackStore;
    private IDanceCategoryService? _danceCategoryService;

    /// <summary>
    /// Virtual root node that wraps the actual tree categories.
    /// </summary>
    private DanceCategoryNode? _virtualRoot;

    public ObservableCollection<Track> Tracks { get; } = [];
    
    // ReSharper disable once CollectionNeverQueried.Local
    private ObservableCollection<DanceCategoryNode> DanceTreeRoot { get; } = [];

    /// <summary>
    /// Single-item collection containing the virtual root for TreeView binding.
    /// </summary>
    public ObservableCollection<DanceCategoryNode> DanceTreeDisplayRoot { get; } = [];

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private bool _isTreeViewMode;

    [ObservableProperty]
    private object? _selectedTreeItem;

    /// <summary>
    /// Whether the selected item is a category (can add children).
    /// </summary>
    public bool CanAddToSelected => SelectedTreeItem is DanceCategoryNode;

    /// <summary>
    /// Whether the selected item can be deleted (not the root).
    /// </summary>
    public bool CanDeleteSelected => SelectedTreeItem != null &&
                                     SelectedTreeItem is not DanceCategoryNode { IsRoot: true };

    /// <summary>
    /// Whether there are actual dance tree entries to export (excludes virtual root).
    /// Checks the virtual root's children since that reflects the actual tree structure.
    /// </summary>
    public bool HasDanceTreeEntries => _virtualRoot?.Children?.Count > 0;

    /// <summary>
    /// Gets the selected dance node for weighted random selection.
    /// For DanceCategoryNode: returns that node.
    /// For DanceItem: returns a temporary wrapper node containing just that dance.
    /// Returns the virtual root if nothing is selected.
    /// </summary>
    public DanceCategoryNode? SelectedDanceNode
    {
        get
        {
            if (SelectedTreeItem is DanceCategoryNode category)
                return category;

            if (SelectedTreeItem is DanceItem dance)
            {
                // Create a temporary node containing just this dance
                return new DanceCategoryNode
                {
                    Name = dance.Name,
                    Weight = 1,
                    Dances = [dance]
                };
            }

            return _virtualRoot;
        }
    }

    public bool CanUndo => _historyService?.CanUndo ?? false;
    public bool CanRedo => _historyService?.CanRedo ?? false;

    public event EventHandler? ImportDanceTreeRequested;
    public event EventHandler? ExportDanceTreeRequested;

    /// <summary>
    /// Event to request showing add category dialog (handled by View).
    /// </summary>
    public event EventHandler? AddCategoryRequested;

    /// <summary>
    /// Event to request showing add dance dialog (handled by View).
    /// </summary>
    public event EventHandler? AddDanceRequested;

    /// <summary>
    /// Event to request showing delete confirmation dialog (handled by View).
    /// </summary>
    public event EventHandler<(object Item, DanceCategoryNode Parent)>? DeleteItemRequested;

    /// <summary>
    /// Design-time constructor
    /// </summary>
    public TrackListViewModel() : this(null!, null!)
    {
        if (Design.IsDesignMode)
        {
            LoadDesignTimeData();
        }
    }

    public TrackListViewModel(IDanceTreeHistoryService historyService, ICommandBus commandBus)
    {
        _historyService = historyService;
        _commandBus = commandBus;

        if (_historyService != null)
        {
            _historyService.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName is nameof(IDanceTreeHistoryService.CanUndo) or nameof(IDanceTreeHistoryService.CanRedo))
                {
                    OnPropertyChanged(nameof(CanUndo));
                    OnPropertyChanged(nameof(CanRedo));
                    UndoCommand.NotifyCanExecuteChanged();
                    RedoCommand.NotifyCanExecuteChanged();
                }
            };
        }
    }

    partial void OnSelectedTreeItemChanged(object? value)
    {
        _ = value; // Suppress unused parameter warning - provided by codegen
        OnPropertyChanged(nameof(CanAddToSelected));
        OnPropertyChanged(nameof(CanDeleteSelected));
    }

    /// <summary>
    /// Sets the TrackStoreService and subscribes to its events
    /// </summary>
    public void SetTrackStore(ITrackStoreService trackStore)
    {
        // Unsubscribe from previous store if any
        if (_trackStore != null)
        {
            _trackStore.TracksReloaded -= OnTracksReloaded;
            _trackStore.TrackAdded -= OnTrackAdded;
            _trackStore.TrackRemoved -= OnTrackRemoved;
        }

        _trackStore = trackStore;

        // Subscribe to new store events
        _trackStore.TracksReloaded += OnTracksReloaded;
        _trackStore.TrackAdded += OnTrackAdded;
        _trackStore.TrackRemoved += OnTrackRemoved;

        // Initial load
        FilterTracks();
    }

    private void OnTracksReloaded(object? sender, EventArgs e)
    {
        FilterTracks();
    }

    private void OnTrackAdded(object? sender, Track track)
    {
        // Re-filter to include the new track if it matches
        FilterTracks();
    }

    private void OnTrackRemoved(object? sender, Track track)
    {
        Tracks.Remove(track);
    }

    public void OnTrackDoubleClicked(Track track)
    {
        _commandBus?.SendAsync(new AddTrackToQueueCommand(track));
    }

    [RelayCommand]
    private void ClearSearch()
    {
        SearchText = string.Empty;
    }

    [RelayCommand]
    private void ToggleViewMode()
    {
        IsTreeViewMode = !IsTreeViewMode;
    }

    [RelayCommand]
    private void RequestImportDanceTree()
    {
        ImportDanceTreeRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand(CanExecute = nameof(HasDanceTreeEntries))]
    private void RequestExportDanceTree()
    {
        ExportDanceTreeRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand(CanExecute = nameof(CanUndo))]
    private void Undo()
    {
        _historyService?.Undo();
        SaveDanceTree();
    }

    [RelayCommand(CanExecute = nameof(CanRedo))]
    private void Redo()
    {
        _historyService?.Redo();
        SaveDanceTree();
    }

    [RelayCommand(CanExecute = nameof(CanAddToSelected))]
    private void RequestAddCategory()
    {
        AddCategoryRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand(CanExecute = nameof(CanAddToSelected))]
    private void RequestAddDance()
    {
        AddDanceRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand(CanExecute = nameof(CanDeleteSelected))]
    private void RequestDeleteItem()
    {
        if (SelectedTreeItem == null || _virtualRoot == null)
            return;

        var parent = FindParentNode(_virtualRoot, SelectedTreeItem);
        if (parent != null)
        {
            DeleteItemRequested?.Invoke(this, (SelectedTreeItem, parent));
        }
    }

    /// <summary>
    /// Called by View after user confirms adding a category.
    /// </summary>
    public void AddCategory(string name, int weight)
    {
        if (SelectedTreeItem is not DanceCategoryNode parentNode)
            return;

        var command = new AddCategoryCommand(parentNode, name, weight, () => { });
        _historyService?.ExecuteCommand(command);
        SaveDanceTree();

        UndoCommand.NotifyCanExecuteChanged();
        RedoCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// Called by View after user confirms adding a dance.
    /// </summary>
    public void AddDance(string name, int weight)
    {
        if (SelectedTreeItem is not DanceCategoryNode parentNode)
            return;

        var command = new AddDanceCommand(parentNode, name, weight, () => { });
        _historyService?.ExecuteCommand(command);
        SaveDanceTree();

        UndoCommand.NotifyCanExecuteChanged();
        RedoCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// Called by View after user confirms deletion.
    /// </summary>
    public void DeleteItem(object item, DanceCategoryNode parent)
    {
        IDanceTreeCommand command;

        if (item is DanceCategoryNode category)
        {
            command = new DeleteCategoryCommand(parent, category, () => { });
        }
        else if (item is DanceItem dance)
        {
            command = new DeleteDanceCommand(parent, dance, () => { });
        }
        else
        {
            return;
        }

        _historyService?.ExecuteCommand(command);
        SelectedTreeItem = null;
        SaveDanceTree();

        UndoCommand.NotifyCanExecuteChanged();
        RedoCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// Checks if deleting the item requires confirmation (non-empty category).
    /// </summary>
    public bool RequiresDeleteConfirmation(object item)
    {
        if (item is DanceCategoryNode category)
        {
            return (category.Children?.Count > 0) || (category.Dances?.Count > 0);
        }
        return false;
    }

    /// <summary>
    /// Gets a description of what will be deleted.
    /// </summary>
    public string GetDeleteDescription(object item)
    {
        if (item is DanceCategoryNode category)
        {
            var childCount = category.Children?.Count ?? 0;
            var danceCount = category.Dances?.Count ?? 0;
            var total = childCount + danceCount;

            if (total > 0)
            {
                return $"Are you sure you want to delete '{category.Name}' and all its contents ({total} items)?";
            }
            return $"Are you sure you want to delete '{category.Name}'?";
        }
        else if (item is DanceItem dance)
        {
            return $"Are you sure you want to delete '{dance.Name}'?";
        }
        return "Are you sure you want to delete this item?";
    }

    private DanceCategoryNode? FindParentNode(DanceCategoryNode current, object target)
    {
        // Check if target is a direct child
        if (current.Children != null)
        {
            foreach (var child in current.Children)
            {
                if (ReferenceEquals(child, target))
                    return current;

                var found = FindParentNode(child, target);
                if (found != null)
                    return found;
            }
        }

        if (current.Dances != null)
        {
            foreach (var dance in current.Dances)
            {
                if (ReferenceEquals(dance, target))
                    return current;
            }
        }

        return null;
    }

    private void SaveDanceTree()
    {
        if (_danceCategoryService == null || _virtualRoot == null)
            return;

        // The actual categories are the children of the virtual root
        _danceCategoryService.Save();
        
        // Notify that export availability may have changed
        OnPropertyChanged(nameof(HasDanceTreeEntries));
        RequestExportDanceTreeCommand.NotifyCanExecuteChanged();
    }

    partial void OnSearchTextChanged(string value)
    {
        _ = value; // Suppress unused parameter warning - provided by codegen
        FilterTracks();
    }

    private void FilterTracks()
    {
        Tracks.Clear();

        if (_trackStore == null)
            return;

        if (string.IsNullOrWhiteSpace(SearchText))
        {
            foreach (var track in _trackStore.Tracks)
            {
                Tracks.Add(track);
            }
            return;
        }

        var normalizedSearch = RemoveDiacritics(SearchText.ToLowerInvariant());

        foreach (var track in _trackStore.Tracks)
        {
            if (TrackMatchesSearch(track, normalizedSearch))
            {
                Tracks.Add(track);
            }
        }
    }

    private bool TrackMatchesSearch(Track track, string normalizedSearch)
    {
        var normalizedDance = RemoveDiacritics(track.Dance.ToLowerInvariant());
        var normalizedArtist = RemoveDiacritics(track.Artist.ToLowerInvariant());
        var normalizedTitle = RemoveDiacritics(track.Title.ToLowerInvariant());

        return normalizedDance.Contains(normalizedSearch) ||
               normalizedArtist.Contains(normalizedSearch) ||
               normalizedTitle.Contains(normalizedSearch);
    }

    private static string RemoveDiacritics(string text)
    {
        var normalized = text.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();

        foreach (var c in normalized)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                sb.Append(c);
            }
        }

        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    private void LoadDesignTimeData()
    {
        // Sample data for designer preview only
        Tracks.Add(new Track("Mazurka", "Sample Artist", "Sample Track 1", TimeSpan.FromSeconds(195), ""));

        Tracks.Add(new Track("Schottische", "Another Artist", "Sample Track 2", TimeSpan.FromSeconds(240), ""));

        Tracks.Add(new Track("Waltz", "Folk Band", "Sample Track 3", TimeSpan.FromSeconds(180), ""));

        Tracks.Add(new Track("Bourrée", "Traditional", "La Bourrée d'Achille", TimeSpan.FromSeconds(210), ""));

        Tracks.Add(new Track("Polska", "Nordic Ensemble", "Bingsjöpolska", TimeSpan.FromSeconds(185), ""));

        // Sample dance tree for designer preview
        var couplesCategory = new DanceCategoryNode
        {
            Name = "Couples",
            Weight = 100,
            Children =
            [
                new DanceCategoryNode
                {
                    Name = "Turning",
                    Weight = 50,
                    Dances =
                    [
                        new DanceItem { Name = "Waltz", Weight = 30 },
                        new DanceItem { Name = "Mazurka", Weight = 25 },
                        new DanceItem { Name = "Schottische", Weight = 20 }
                    ]
                },

                new DanceCategoryNode
                {
                    Name = "Set Dances",
                    Weight = 30,
                    Dances =
                    [
                        new DanceItem { Name = "Contra", Weight = 15 },
                        new DanceItem { Name = "Square", Weight = 10 }
                    ]
                }
            ]
        };

        var lineCategory = new DanceCategoryNode
        {
            Name = "Line Dances",
            Weight = 40,
            Dances =
            [
                new DanceItem { Name = "Circassian Circle", Weight = 20 },
                new DanceItem { Name = "Breton An Dro", Weight = 15 }
            ]
        };

        DanceTreeRoot.Add(couplesCategory);
        DanceTreeRoot.Add(lineCategory);
    }

    /// <summary>
    /// Sets the DanceCategoryService and loads the dance tree
    /// </summary>
    public void SetDanceCategoryService(IDanceCategoryService danceCategoryService)
    {
        _danceCategoryService = danceCategoryService;
        LoadDanceTree();
    }

    /// <summary>
    /// Imports a dance tree from the specified file path
    /// </summary>
    public bool ImportDanceTree(string filePath)
    {
        if (_danceCategoryService == null)
            return false;

        var result = _danceCategoryService.Import(filePath);
        if (result)
        {
            LoadDanceTree();
            _historyService?.Clear(); // Clear history after import
        }
        return result;
    }

    /// <summary>
    /// Exports the dance tree to the specified file path
    /// </summary>
    public bool ExportDanceTree(string filePath)
    {
        return _danceCategoryService?.Export(filePath) ?? false;
    }

    private void LoadDanceTree()
    {
        DanceTreeRoot.Clear();
        DanceTreeDisplayRoot.Clear();

        if (_danceCategoryService == null)
            return;

        // Get the categories list - this is a reference to the service's internal list
        var categories = _danceCategoryService.Load();
        foreach (var category in categories)
        {
            DanceTreeRoot.Add(category);
        }

        // Create virtual root node - use the SAME list reference, not a copy!
        // This ensures edits to _virtualRoot.Children also modify the service's data
        _virtualRoot = new DanceCategoryNode
        {
            Name = "Dance",
            IsRoot = true,
            Children = categories  // Direct reference, NOT [..categories] which copies!
        };
        _virtualRoot.RefreshItems();

        DanceTreeDisplayRoot.Add(_virtualRoot);

        // Select the root node by default
        SelectedTreeItem = _virtualRoot;
        
        // Notify that export availability may have changed
        OnPropertyChanged(nameof(HasDanceTreeEntries));
        RequestExportDanceTreeCommand.NotifyCanExecuteChanged();
    }
}
