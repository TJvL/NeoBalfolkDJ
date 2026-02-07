using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace NeoBalfolkDJ.Services;

/// <summary>
/// Service to manage undo/redo history for dance tree commands.
/// </summary>
public sealed class DanceTreeHistoryService(ILoggingService logger) : IDanceTreeHistoryService, IDisposable
{
    private readonly ILoggingService _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly Stack<IDanceTreeCommand> _undoStack = new();
    private readonly Stack<IDanceTreeCommand> _redoStack = new();
    private bool _disposed;

    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;

    public string? UndoDescription => _undoStack.Count > 0 ? _undoStack.Peek().Description : null;
    public string? RedoDescription => _redoStack.Count > 0 ? _redoStack.Peek().Description : null;

    /// <summary>
    /// Executes a command and adds it to the undo stack.
    /// Clears the redo stack.
    /// </summary>
    public void ExecuteCommand(IDanceTreeCommand command)
    {
        if (_disposed) return;

        command.Execute();
        _undoStack.Push(command);
        _redoStack.Clear();
        NotifyChanges();

        _logger.Debug($"DanceTreeHistory: Executed '{command.Description}'");
    }

    /// <summary>
    /// Undoes the last command.
    /// </summary>
    public void Undo()
    {
        if (_disposed || !CanUndo)
            return;

        var command = _undoStack.Pop();
        command.Undo();
        _redoStack.Push(command);
        NotifyChanges();

        _logger.Debug($"DanceTreeHistory: Undid '{command.Description}'");
    }

    /// <summary>
    /// Redoes the last undone command.
    /// </summary>
    public void Redo()
    {
        if (_disposed || !CanRedo)
            return;

        var command = _redoStack.Pop();
        command.Execute();
        _undoStack.Push(command);
        NotifyChanges();

        _logger.Debug($"DanceTreeHistory: Redid '{command.Description}'");
    }

    /// <summary>
    /// Clears all history.
    /// </summary>
    public void Clear()
    {
        if (_disposed) return;

        _undoStack.Clear();
        _redoStack.Clear();
        NotifyChanges();
    }

    private void NotifyChanges()
    {
        OnPropertyChanged(nameof(CanUndo));
        OnPropertyChanged(nameof(CanRedo));
        OnPropertyChanged(nameof(UndoDescription));
        OnPropertyChanged(nameof(RedoDescription));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        PropertyChanged = null;
        _undoStack.Clear();
        _redoStack.Clear();
    }
}
