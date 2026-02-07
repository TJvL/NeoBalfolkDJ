using System;
using System.Collections.Generic;
using NeoBalfolkDJ.Models;

namespace NeoBalfolkDJ.Services;

/// <summary>
/// Interface for dance tree commands supporting undo/redo.
/// </summary>
public interface IDanceTreeCommand
{
    void Execute();
    void Undo();
    string Description { get; }
}

/// <summary>
/// Command to add a new category to a parent node.
/// </summary>
public class AddCategoryCommand(DanceCategoryNode parent, string name, int weight, Action onChanged)
    : IDanceTreeCommand
{
    private readonly DanceCategoryNode _newCategory = new() { Name = name, Weight = weight };

    public string Description => $"Add category '{_newCategory.Name}'";

    public void Execute()
    {
        parent.Children ??= new List<DanceCategoryNode>();
        parent.Children.Add(_newCategory);
        parent.RefreshItems();
        onChanged();
    }

    public void Undo()
    {
        parent.Children?.Remove(_newCategory);
        if (parent.Children?.Count == 0)
            parent.Children = null;
        parent.RefreshItems();
        onChanged();
    }
}

/// <summary>
/// Command to add a new dance to a parent node.
/// </summary>
public class AddDanceCommand(DanceCategoryNode parent, string name, int weight, Action onChanged)
    : IDanceTreeCommand
{
    private readonly DanceItem _newDance = new() { Name = name, Weight = weight };

    public string Description => $"Add dance '{_newDance.Name}'";

    public void Execute()
    {
        parent.Dances ??= new List<DanceItem>();
        parent.Dances.Add(_newDance);
        parent.RefreshItems();
        onChanged();
    }

    public void Undo()
    {
        parent.Dances?.Remove(_newDance);
        if (parent.Dances?.Count == 0)
            parent.Dances = null;
        parent.RefreshItems();
        onChanged();
    }
}

/// <summary>
/// Command to delete a category node and all its children.
/// </summary>
public class DeleteCategoryCommand(DanceCategoryNode parent, DanceCategoryNode category, Action onChanged)
    : IDanceTreeCommand
{
    private readonly int _index = parent.Children?.IndexOf(category) ?? -1;

    public string Description => $"Delete category '{category.Name}'";

    public void Execute()
    {
        parent.Children?.Remove(category);
        if (parent.Children?.Count == 0)
            parent.Children = null;
        parent.RefreshItems();
        onChanged();
    }

    public void Undo()
    {
        parent.Children ??= new List<DanceCategoryNode>();
        if (_index >= 0 && _index <= parent.Children.Count)
            parent.Children.Insert(_index, category);
        else
            parent.Children.Add(category);
        parent.RefreshItems();
        onChanged();
    }
}

/// <summary>
/// Command to delete a dance item.
/// </summary>
public class DeleteDanceCommand(DanceCategoryNode parent, DanceItem dance, Action onChanged)
    : IDanceTreeCommand
{
    private readonly int _index = parent.Dances?.IndexOf(dance) ?? -1;

    public string Description => $"Delete dance '{dance.Name}'";

    public void Execute()
    {
        parent.Dances?.Remove(dance);
        if (parent.Dances?.Count == 0)
            parent.Dances = null;
        parent.RefreshItems();
        onChanged();
    }

    public void Undo()
    {
        parent.Dances ??= new List<DanceItem>();
        if (_index >= 0 && _index <= parent.Dances.Count)
            parent.Dances.Insert(_index, dance);
        else
            parent.Dances.Add(dance);
        parent.RefreshItems();
        onChanged();
    }
}
