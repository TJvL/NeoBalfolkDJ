using System.ComponentModel;

namespace NeoBalfolkDJ.Services;

/// <summary>
/// Service to manage undo/redo history for dance tree commands.
/// </summary>
public interface IDanceTreeHistoryService : INotifyPropertyChanged
{
    bool CanUndo { get; }
    bool CanRedo { get; }
    string? UndoDescription { get; }
    string? RedoDescription { get; }

    /// <summary>
    /// Executes a command and adds it to the undo stack.
    /// </summary>
    void ExecuteCommand(IDanceTreeCommand command);

    /// <summary>
    /// Undoes the last command.
    /// </summary>
    void Undo();

    /// <summary>
    /// Redoes the last undone command.
    /// </summary>
    void Redo();

    /// <summary>
    /// Clears all history.
    /// </summary>
    void Clear();
}


