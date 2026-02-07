using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeoBalfolkDJ.Models;
using NeoBalfolkDJ.Services;

namespace NeoBalfolkDJ.ViewModels;

/// <summary>
/// ViewModel for a single synonym entry (a dance name and its synonyms).
/// </summary>
public partial class DanceSynonymEntryViewModel : ViewModelBase
{
    private readonly DanceSynonymEditorViewModel _parent;

    [ObservableProperty]
    private string _name;

    [ObservableProperty]
    private bool _isAddingSynonym;

    [ObservableProperty]
    private string _newSynonymText = string.Empty;

    public ObservableCollection<string> Synonyms { get; } = new();

    public DanceSynonymEntryViewModel(DanceSynonymEditorViewModel parent, DanceSynonym model)
    {
        _parent = parent;
        Model = model;
        _name = model.Name;

        foreach (var synonym in model.Synonyms)
        {
            Synonyms.Add(synonym);
        }
    }

    private DanceSynonym Model { get; }

    partial void OnNameChanged(string? oldValue, string newValue)
    {
        if (oldValue != null && oldValue != newValue)
        {
            _parent.RenameEntry(this, newValue);
        }
    }

    [RelayCommand]
    private void StartAddSynonym()
    {
        NewSynonymText = string.Empty;
        IsAddingSynonym = true;
    }

    [RelayCommand]
    private void ConfirmAddSynonym()
    {
        if (!string.IsNullOrWhiteSpace(NewSynonymText))
        {
            _parent.AddSynonym(this, NewSynonymText.Trim());
        }
        IsAddingSynonym = false;
        NewSynonymText = string.Empty;
    }

    [RelayCommand]
    private void CancelAddSynonym()
    {
        IsAddingSynonym = false;
        NewSynonymText = string.Empty;
    }

    [RelayCommand]
    private void RemoveSynonym(string synonym)
    {
        _parent.RemoveSynonym(this, synonym);
    }

    [RelayCommand]
    private void RequestRemoveLine()
    {
        _parent.RequestRemoveLine(this);
    }

    /// <summary>
    /// Refreshes the synonyms from the model.
    /// </summary>
    public void RefreshFromModel()
    {
        Name = Model.Name;
        Synonyms.Clear();
        foreach (var synonym in Model.Synonyms)
        {
            Synonyms.Add(synonym);
        }
    }
}

/// <summary>
/// ViewModel for the Dance Synonym Editor dialog.
/// </summary>
public partial class DanceSynonymEditorViewModel : ViewModelBase, IDisposable
{
    private readonly IDanceSynonymService _service;
    private readonly INotificationService? _notificationService;

    public ObservableCollection<DanceSynonymEntryViewModel> Entries { get; } = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(UndoCommand))]
    private bool _canUndo;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RedoCommand))]
    private bool _canRedo;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ExportCommand))]
    private bool _hasEntries;

    /// <summary>
    /// Event raised when import is requested (View shows file picker then confirmation).
    /// </summary>
    public event EventHandler? ImportRequested;

    /// <summary>
    /// Event raised when export is requested (View shows file picker).
    /// </summary>
    public event EventHandler? ExportRequested;

    /// <summary>
    /// Event raised when line deletion confirmation is needed.
    /// </summary>
    public event EventHandler<DanceSynonymEntryViewModel>? DeleteLineConfirmationRequested;

    public DanceSynonymEditorViewModel(IDanceSynonymService service, INotificationService? notificationService = null)
    {
        _service = service;
        _notificationService = notificationService;

        // Subscribe to service events
        _service.DataChanged += OnDataChanged;
        _service.UndoRedoStateChanged += OnUndoRedoStateChanged;

        // Load initial data
        RefreshFromService();
        UpdateUndoRedoState();
    }

    private void OnDataChanged(object? sender, EventArgs e)
    {
        RefreshFromService();
    }

    private void OnUndoRedoStateChanged(object? sender, EventArgs e)
    {
        UpdateUndoRedoState();
    }

    private void UpdateUndoRedoState()
    {
        CanUndo = _service.CanUndo;
        CanRedo = _service.CanRedo;
    }

    private void RefreshFromService()
    {
        Entries.Clear();
        foreach (var synonym in _service.Synonyms)
        {
            Entries.Add(new DanceSynonymEntryViewModel(this, synonym));
        }
        HasEntries = Entries.Count > 0;
    }

    [RelayCommand]
    private void AddLine()
    {
        // Find a unique default name
        var baseName = "New Dance";
        var name = baseName;
        var counter = 1;

        while (_service.IsDuplicate(name))
        {
            name = $"{baseName} {counter++}";
        }

        _service.AddEntry(new DanceSynonym { Name = name });
    }

    public void RequestRemoveLine(DanceSynonymEntryViewModel entry)
    {
        if (entry.Synonyms.Count > 0)
        {
            // Has synonyms, request confirmation
            DeleteLineConfirmationRequested?.Invoke(this, entry);
        }
        else
        {
            // No synonyms, delete directly
            ConfirmRemoveLine(entry);
        }
    }

    public void ConfirmRemoveLine(DanceSynonymEntryViewModel entry)
    {
        var index = Entries.IndexOf(entry);
        if (index >= 0)
        {
            _service.RemoveEntry(index);
        }
    }

    public void RenameEntry(DanceSynonymEntryViewModel entry, string newName)
    {
        // Check for duplicates
        var index = Entries.IndexOf(entry);
        if (_service.IsDuplicate(newName, index))
        {
            // Revert to old name
            entry.RefreshFromModel();
            _notificationService?.ShowNotification("This name already exists", NotificationSeverity.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(newName))
        {
            entry.RefreshFromModel();
            _notificationService?.ShowNotification("Name cannot be empty", NotificationSeverity.Warning);
            return;
        }

        _service.UpdateEntryName(index, newName);
    }

    public void AddSynonym(DanceSynonymEntryViewModel entry, string synonym)
    {
        // Check for empty
        if (string.IsNullOrWhiteSpace(synonym))
        {
            _notificationService?.ShowNotification("Synonym cannot be empty", NotificationSeverity.Warning);
            return;
        }

        // Check for duplicates across all entries
        if (_service.IsDuplicate(synonym))
        {
            _notificationService?.ShowNotification("This synonym already exists", NotificationSeverity.Warning);
            return;
        }

        var index = Entries.IndexOf(entry);
        if (index >= 0)
        {
            _service.AddSynonymToEntry(index, synonym);
        }
    }

    public void RemoveSynonym(DanceSynonymEntryViewModel entry, string synonym)
    {
        var index = Entries.IndexOf(entry);
        if (index >= 0)
        {
            _service.RemoveSynonymFromEntry(index, synonym);
        }
    }

    [RelayCommand(CanExecute = nameof(CanUndo))]
    private void Undo()
    {
        _service.Undo();
    }

    [RelayCommand(CanExecute = nameof(CanRedo))]
    private void Redo()
    {
        _service.Redo();
    }

    [RelayCommand]
    private void Import()
    {
        ImportRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand(CanExecute = nameof(HasEntries))]
    private void Export()
    {
        ExportRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Called by View after import file is selected and user confirms overwrite.
    /// </summary>
    public void PerformImport(string filePath)
    {
        _service.Import(filePath);
    }

    /// <summary>
    /// Called by View after export file is selected.
    /// </summary>
    public void PerformExport(string filePath)
    {
        _service.Export(filePath);
    }

    public void Dispose()
    {
        _service.DataChanged -= OnDataChanged;
        _service.UndoRedoStateChanged -= OnUndoRedoStateChanged;
    }
}

