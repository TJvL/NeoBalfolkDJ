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
public class AddCategoryCommand : IDanceTreeCommand
{
    private readonly DanceCategoryNode _parent;
    private readonly DanceCategoryNode _newCategory;
    private readonly Action _onChanged;
    
    public string Description => $"Add category '{_newCategory.Name}'";
    
    public AddCategoryCommand(DanceCategoryNode parent, string name, int weight, Action onChanged)
    {
        _parent = parent;
        _newCategory = new DanceCategoryNode { Name = name, Weight = weight };
        _onChanged = onChanged;
    }
    
    public void Execute()
    {
        _parent.Children ??= new List<DanceCategoryNode>();
        _parent.Children.Add(_newCategory);
        _parent.RefreshItems();
        _onChanged();
    }
    
    public void Undo()
    {
        _parent.Children?.Remove(_newCategory);
        if (_parent.Children?.Count == 0)
            _parent.Children = null;
        _parent.RefreshItems();
        _onChanged();
    }
}

/// <summary>
/// Command to add a new dance to a parent node.
/// </summary>
public class AddDanceCommand : IDanceTreeCommand
{
    private readonly DanceCategoryNode _parent;
    private readonly DanceItem _newDance;
    private readonly Action _onChanged;
    
    public string Description => $"Add dance '{_newDance.Name}'";
    
    public AddDanceCommand(DanceCategoryNode parent, string name, int weight, Action onChanged)
    {
        _parent = parent;
        _newDance = new DanceItem { Name = name, Weight = weight };
        _onChanged = onChanged;
    }
    
    public void Execute()
    {
        _parent.Dances ??= new List<DanceItem>();
        _parent.Dances.Add(_newDance);
        _parent.RefreshItems();
        _onChanged();
    }
    
    public void Undo()
    {
        _parent.Dances?.Remove(_newDance);
        if (_parent.Dances?.Count == 0)
            _parent.Dances = null;
        _parent.RefreshItems();
        _onChanged();
    }
}

/// <summary>
/// Command to delete a category node and all its children.
/// </summary>
public class DeleteCategoryCommand : IDanceTreeCommand
{
    private readonly DanceCategoryNode _parent;
    private readonly DanceCategoryNode _category;
    private readonly int _index;
    private readonly Action _onChanged;
    
    public string Description => $"Delete category '{_category.Name}'";
    
    public DeleteCategoryCommand(DanceCategoryNode parent, DanceCategoryNode category, Action onChanged)
    {
        _parent = parent;
        _category = category;
        _index = parent.Children?.IndexOf(category) ?? -1;
        _onChanged = onChanged;
    }
    
    public void Execute()
    {
        _parent.Children?.Remove(_category);
        if (_parent.Children?.Count == 0)
            _parent.Children = null;
        _parent.RefreshItems();
        _onChanged();
    }
    
    public void Undo()
    {
        _parent.Children ??= new List<DanceCategoryNode>();
        if (_index >= 0 && _index <= _parent.Children.Count)
            _parent.Children.Insert(_index, _category);
        else
            _parent.Children.Add(_category);
        _parent.RefreshItems();
        _onChanged();
    }
}

/// <summary>
/// Command to delete a dance item.
/// </summary>
public class DeleteDanceCommand : IDanceTreeCommand
{
    private readonly DanceCategoryNode _parent;
    private readonly DanceItem _dance;
    private readonly int _index;
    private readonly Action _onChanged;
    
    public string Description => $"Delete dance '{_dance.Name}'";
    
    public DeleteDanceCommand(DanceCategoryNode parent, DanceItem dance, Action onChanged)
    {
        _parent = parent;
        _dance = dance;
        _index = parent.Dances?.IndexOf(dance) ?? -1;
        _onChanged = onChanged;
    }
    
    public void Execute()
    {
        _parent.Dances?.Remove(_dance);
        if (_parent.Dances?.Count == 0)
            _parent.Dances = null;
        _parent.RefreshItems();
        _onChanged();
    }
    
    public void Undo()
    {
        _parent.Dances ??= new List<DanceItem>();
        if (_index >= 0 && _index <= _parent.Dances.Count)
            _parent.Dances.Insert(_index, _dance);
        else
            _parent.Dances.Add(_dance);
        _parent.RefreshItems();
        _onChanged();
    }
}
