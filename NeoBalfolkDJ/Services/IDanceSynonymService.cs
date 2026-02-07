using System;
using System.Collections.Generic;
using NeoBalfolkDJ.Models;

namespace NeoBalfolkDJ.Services;

/// <summary>
/// Service for loading, saving, importing, and exporting dance synonyms.
/// </summary>
public interface IDanceSynonymService
{
    /// <summary>
    /// Gets the current loaded synonyms.
    /// </summary>
    IReadOnlyList<DanceSynonym> Synonyms { get; }

    /// <summary>
    /// Whether undo is available.
    /// </summary>
    bool CanUndo { get; }

    /// <summary>
    /// Whether redo is available.
    /// </summary>
    bool CanRedo { get; }

    /// <summary>
    /// Event raised when data changes.
    /// </summary>
    event EventHandler? DataChanged;

    /// <summary>
    /// Event raised when undo/redo state changes.
    /// </summary>
    event EventHandler? UndoRedoStateChanged;

    /// <summary>
    /// Loads synonyms from user data.
    /// </summary>
    List<DanceSynonym> Load();

    /// <summary>
    /// Saves synonyms to user data.
    /// </summary>
    void Save();

    /// <summary>
    /// Imports synonyms from a file.
    /// </summary>
    bool Import(string filePath);

    /// <summary>
    /// Exports synonyms to a file.
    /// </summary>
    bool Export(string filePath);

    /// <summary>
    /// Checks if a name/synonym already exists.
    /// </summary>
    bool IsDuplicate(string value, int? excludeEntryIndex = null, string? excludeSynonymInEntry = null);

    /// <summary>
    /// Adds a new synonym entry.
    /// </summary>
    void AddEntry(DanceSynonym entry);

    /// <summary>
    /// Removes a synonym entry.
    /// </summary>
    void RemoveEntry(int index);

    /// <summary>
    /// Updates the name of an entry.
    /// </summary>
    void UpdateEntryName(int index, string newName);

    /// <summary>
    /// Adds a synonym to an entry.
    /// </summary>
    void AddSynonymToEntry(int entryIndex, string synonym);

    /// <summary>
    /// Removes a synonym from an entry.
    /// </summary>
    void RemoveSynonymFromEntry(int entryIndex, string synonym);

    /// <summary>
    /// Undoes the last operation.
    /// </summary>
    void Undo();

    /// <summary>
    /// Redoes the last undone operation.
    /// </summary>
    void Redo();
}

