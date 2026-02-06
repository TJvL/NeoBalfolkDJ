using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using NeoBalfolkDJ.Models;

namespace NeoBalfolkDJ.Services;

/// <summary>
/// Command interface for undo/redo functionality.
/// </summary>
public interface ISynonymCommand
{
    void Execute();
    void Undo();
}

/// <summary>
/// Service for loading, saving, importing, and exporting dance synonyms.
/// Stores synonyms in user local application data with a default from embedded resources.
/// Supports undo/redo and auto-save.
/// </summary>
public class DanceSynonymService
{
    private static readonly string DataDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "NeoBalfolkDJ");
    
    private static readonly string SynonymsFilePath = Path.Combine(DataDirectory, "dancesynonyms.json");
    
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
    
    private static readonly JsonSerializerOptions StrictJsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };
    
    private readonly NotificationService? _notificationService;
    private readonly Stack<ISynonymCommand> _undoStack = new();
    private readonly Stack<ISynonymCommand> _redoStack = new();
    
    private List<DanceSynonym> _synonyms = new();
    
    public event EventHandler? DataChanged;
    public event EventHandler? UndoRedoStateChanged;
    
    public NotificationService? NotificationService => _notificationService;
    
    public DanceSynonymService(NotificationService? notificationService = null)
    {
        _notificationService = notificationService;
    }
    
    /// <summary>
    /// Gets the current loaded synonyms.
    /// </summary>
    public IReadOnlyList<DanceSynonym> Synonyms => _synonyms;
    
    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;
    
    /// <summary>
    /// Normalizes a string for comparison by removing accents, special characters,
    /// and converting to lowercase.
    /// </summary>
    public static string NormalizeForComparison(string input)
    {
        return StringNormalizer.NormalizeForComparison(input);
    }
    
    /// <summary>
    /// Checks if a name/synonym already exists in any entry.
    /// </summary>
    /// <param name="value">The value to check</param>
    /// <param name="excludeEntryIndex">Optional entry index to exclude from check</param>
    /// <param name="excludeSynonymInEntry">Optional synonym to exclude within the excluded entry</param>
    /// <returns>True if duplicate exists</returns>
    public bool IsDuplicate(string value, int? excludeEntryIndex = null, string? excludeSynonymInEntry = null)
    {
        var normalizedValue = NormalizeForComparison(value);
        if (string.IsNullOrEmpty(normalizedValue))
            return false;
        
        for (int i = 0; i < _synonyms.Count; i++)
        {
            var entry = _synonyms[i];
            
            // Check the name
            if (i != excludeEntryIndex || excludeSynonymInEntry != null)
            {
                if (NormalizeForComparison(entry.Name) == normalizedValue)
                    return true;
            }
            
            // Check synonyms
            foreach (var synonym in entry.Synonyms)
            {
                // Skip if this is the excluded synonym in the excluded entry
                if (i == excludeEntryIndex && synonym == excludeSynonymInEntry)
                    continue;
                
                if (NormalizeForComparison(synonym) == normalizedValue)
                    return true;
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// Loads synonyms from user data, or extracts the default if not present.
    /// </summary>
    public List<DanceSynonym> Load()
    {
        try
        {
            Directory.CreateDirectory(DataDirectory);
            
            if (!File.Exists(SynonymsFilePath))
            {
                LoggingService.Info("Dance synonyms file not found, extracting default");
                ExtractDefaultSynonyms();
            }
            
            if (File.Exists(SynonymsFilePath))
            {
                var json = File.ReadAllText(SynonymsFilePath);
                var synonyms = JsonSerializer.Deserialize<List<DanceSynonym>>(json, JsonOptions);
                if (synonyms != null)
                {
                    _synonyms = synonyms;
                    LoggingService.Info($"Loaded {_synonyms.Count} dance synonym entries");
                    return _synonyms;
                }
            }
        }
        catch (Exception ex)
        {
            LoggingService.Error("Failed to load dance synonyms", ex);
            _notificationService?.ShowNotification("Failed to load dance synonyms: " + ex.Message, NotificationSeverity.Error);
        }
        
        _synonyms = new List<DanceSynonym>();
        return _synonyms;
    }
    
    /// <summary>
    /// Saves the current synonyms to user data.
    /// </summary>
    public void Save()
    {
        try
        {
            Directory.CreateDirectory(DataDirectory);
            var json = JsonSerializer.Serialize(_synonyms, JsonOptions);
            File.WriteAllText(SynonymsFilePath, json);
            LoggingService.Debug("Dance synonyms saved successfully");
        }
        catch (Exception ex)
        {
            LoggingService.Error("Failed to save dance synonyms", ex);
            _notificationService?.ShowNotification("Failed to save dance synonyms: " + ex.Message, NotificationSeverity.Error);
        }
    }
    
    /// <summary>
    /// Imports synonyms from the specified file path with strict JSON validation.
    /// </summary>
    public bool Import(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                var message = $"Import file not found: {filePath}";
                LoggingService.Warning(message);
                _notificationService?.ShowNotification(message, NotificationSeverity.Warning);
                return false;
            }
            
            var json = File.ReadAllText(filePath);
            var imported = JsonSerializer.Deserialize<List<DanceSynonym>>(json, StrictJsonOptions);
            
            if (imported == null)
            {
                var message = "Import failed: Invalid JSON structure";
                LoggingService.Warning(message);
                _notificationService?.ShowNotification(message, NotificationSeverity.Warning);
                return false;
            }
            
            // Validate structure
            foreach (var entry in imported)
            {
                if (entry.Name == null || entry.Synonyms == null)
                {
                    var message = "Import failed: Invalid entry structure (missing name or synonyms)";
                    LoggingService.Warning(message);
                    _notificationService?.ShowNotification(message, NotificationSeverity.Warning);
                    return false;
                }
            }
            
            // Clear undo/redo stacks on import
            _undoStack.Clear();
            _redoStack.Clear();
            
            _synonyms = imported;
            Save();
            
            LoggingService.Info($"Imported {_synonyms.Count} dance synonym entries from {filePath}");
            _notificationService?.ShowNotification($"Imported {_synonyms.Count} synonym entries", NotificationSeverity.Information);
            
            DataChanged?.Invoke(this, EventArgs.Empty);
            UndoRedoStateChanged?.Invoke(this, EventArgs.Empty);
            
            return true;
        }
        catch (JsonException ex)
        {
            var message = $"Import failed: Invalid JSON format - {ex.Message}";
            LoggingService.Warning(message);
            _notificationService?.ShowNotification("Import failed: Invalid JSON format", NotificationSeverity.Error);
            return false;
        }
        catch (Exception ex)
        {
            LoggingService.Error("Failed to import dance synonyms", ex);
            _notificationService?.ShowNotification("Import failed: " + ex.Message, NotificationSeverity.Error);
            return false;
        }
    }
    
    /// <summary>
    /// Exports synonyms to the specified file path.
    /// </summary>
    public bool Export(string filePath)
    {
        try
        {
            var json = JsonSerializer.Serialize(_synonyms, JsonOptions);
            File.WriteAllText(filePath, json);
            
            LoggingService.Info($"Exported {_synonyms.Count} dance synonym entries to {filePath}");
            _notificationService?.ShowNotification($"Exported {_synonyms.Count} synonym entries", NotificationSeverity.Information);
            
            return true;
        }
        catch (Exception ex)
        {
            LoggingService.Error("Failed to export dance synonyms", ex);
            _notificationService?.ShowNotification("Export failed: " + ex.Message, NotificationSeverity.Error);
            return false;
        }
    }
    
    /// <summary>
    /// Executes a command and adds it to the undo stack.
    /// </summary>
    public void ExecuteCommand(ISynonymCommand command)
    {
        command.Execute();
        _undoStack.Push(command);
        _redoStack.Clear();
        Save();
        DataChanged?.Invoke(this, EventArgs.Empty);
        UndoRedoStateChanged?.Invoke(this, EventArgs.Empty);
    }
    
    /// <summary>
    /// Undoes the last command.
    /// </summary>
    public void Undo()
    {
        if (!CanUndo) return;
        
        var command = _undoStack.Pop();
        command.Undo();
        _redoStack.Push(command);
        Save();
        DataChanged?.Invoke(this, EventArgs.Empty);
        UndoRedoStateChanged?.Invoke(this, EventArgs.Empty);
    }
    
    /// <summary>
    /// Redoes the last undone command.
    /// </summary>
    public void Redo()
    {
        if (!CanRedo) return;
        
        var command = _redoStack.Pop();
        command.Execute();
        _undoStack.Push(command);
        Save();
        DataChanged?.Invoke(this, EventArgs.Empty);
        UndoRedoStateChanged?.Invoke(this, EventArgs.Empty);
    }
    
    /// <summary>
    /// Gets the mutable list for command operations.
    /// </summary>
    internal List<DanceSynonym> GetMutableList() => _synonyms;
    
    private void ExtractDefaultSynonyms()
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "NeoBalfolkDJ.Assets.dancesynonyms.json";
            
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                LoggingService.Error($"Embedded resource '{resourceName}' not found");
                return;
            }
            
            using var reader = new StreamReader(stream);
            var json = reader.ReadToEnd();
            
            File.WriteAllText(SynonymsFilePath, json);
            LoggingService.Info("Default dance synonyms extracted successfully");
        }
        catch (Exception ex)
        {
            LoggingService.Error("Failed to extract default dance synonyms", ex);
        }
    }
}

#region Commands

public class AddLineCommand : ISynonymCommand
{
    private readonly DanceSynonymService _service;
    private readonly DanceSynonym _entry;
    private readonly int _index;
    
    public AddLineCommand(DanceSynonymService service, string name)
    {
        _service = service;
        _entry = new DanceSynonym { Name = name };
        _index = service.GetMutableList().Count;
    }
    
    public void Execute()
    {
        _service.GetMutableList().Add(_entry);
    }
    
    public void Undo()
    {
        _service.GetMutableList().Remove(_entry);
    }
}

public class RemoveLineCommand : ISynonymCommand
{
    private readonly DanceSynonymService _service;
    private readonly DanceSynonym _entry;
    private readonly int _index;
    
    public RemoveLineCommand(DanceSynonymService service, int index)
    {
        _service = service;
        _index = index;
        _entry = service.GetMutableList()[index];
    }
    
    public void Execute()
    {
        _service.GetMutableList().RemoveAt(_index);
    }
    
    public void Undo()
    {
        _service.GetMutableList().Insert(_index, _entry);
    }
}

public class RenameEntryCommand : ISynonymCommand
{
    private readonly DanceSynonym _entry;
    private readonly string _oldName;
    private readonly string _newName;
    
    public RenameEntryCommand(DanceSynonym entry, string newName)
    {
        _entry = entry;
        _oldName = entry.Name;
        _newName = newName;
    }
    
    public void Execute()
    {
        _entry.Name = _newName;
    }
    
    public void Undo()
    {
        _entry.Name = _oldName;
    }
}

public class AddSynonymCommand : ISynonymCommand
{
    private readonly DanceSynonym _entry;
    private readonly string _synonym;
    
    public AddSynonymCommand(DanceSynonym entry, string synonym)
    {
        _entry = entry;
        _synonym = synonym;
    }
    
    public void Execute()
    {
        _entry.Synonyms.Add(_synonym);
    }
    
    public void Undo()
    {
        _entry.Synonyms.Remove(_synonym);
    }
}

public class RemoveSynonymCommand : ISynonymCommand
{
    private readonly DanceSynonym _entry;
    private readonly string _synonym;
    private readonly int _index;
    
    public RemoveSynonymCommand(DanceSynonym entry, string synonym)
    {
        _entry = entry;
        _synonym = synonym;
        _index = entry.Synonyms.IndexOf(synonym);
    }
    
    public void Execute()
    {
        _entry.Synonyms.Remove(_synonym);
    }
    
    public void Undo()
    {
        if (_index >= 0 && _index <= _entry.Synonyms.Count)
            _entry.Synonyms.Insert(_index, _synonym);
        else
            _entry.Synonyms.Add(_synonym);
    }
}

#endregion


