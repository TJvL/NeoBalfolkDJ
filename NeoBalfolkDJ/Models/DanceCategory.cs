using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace NeoBalfolkDJ.Models;

/// <summary>
/// Represents a leaf dance item in the dance category tree.
/// </summary>
public sealed class DanceItem : INotifyPropertyChanged
{
    private string _name = string.Empty;
    private int _weight;

    [JsonPropertyName("name")]
    public string Name
    {
        get => _name;
        // ReSharper disable once PropertyCanBeMadeInitOnly.Global
        set
        {
            if (_name != value)
            {
                _name = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DisplayName));
            }
        }
    }

    [JsonPropertyName("weight")]
    public int Weight
    {
        get => _weight;
        // ReSharper disable once PropertyCanBeMadeInitOnly.Global
        set
        {
            if (_weight != value)
            {
                _weight = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DisplayName));
            }
        }
    }

    /// <summary>
    /// Tracks assigned to this dance (not serialized).
    /// </summary>
    [JsonIgnore]
    public List<Track> AssignedTracks { get; } = new();

    /// <summary>
    /// Number of tracks assigned to this dance.
    /// </summary>
    [JsonIgnore]
    public int TrackCount => AssignedTracks.Count;

    /// <summary>
    /// Whether this dance is effectively disabled (weight 0 or no tracks).
    /// </summary>
    [JsonIgnore]
    public bool IsEffectivelyDisabled => Weight == 0 || TrackCount == 0;

    /// <summary>
    /// Display name with weight and track count for UI binding.
    /// </summary>
    [JsonIgnore]
    public string DisplayName => $"{Name}  Weight: {Weight} ({TrackCount} tracks)";

    /// <summary>
    /// Clears all assigned tracks.
    /// </summary>
    public void ClearAssignedTracks()
    {
        AssignedTracks.Clear();
        OnPropertyChanged(nameof(TrackCount));
        OnPropertyChanged(nameof(DisplayName));
        OnPropertyChanged(nameof(IsEffectivelyDisabled));
    }

    /// <summary>
    /// Assigns a track to this dance.
    /// </summary>
    public void AssignTrack(Track track)
    {
        AssignedTracks.Add(track);
        OnPropertyChanged(nameof(TrackCount));
        OnPropertyChanged(nameof(DisplayName));
        OnPropertyChanged(nameof(IsEffectivelyDisabled));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

/// <summary>
/// Represents a category node in the dance category tree.
/// Can contain both child categories and leaf dance items.
/// </summary>
public sealed class DanceCategoryNode : INotifyPropertyChanged
{
    private string _name = string.Empty;
    private int _weight;

    [JsonPropertyName("name")]
    public string Name
    {
        get => _name;
        // ReSharper disable once PropertyCanBeMadeInitOnly.Global
        set
        {
            if (_name != value)
            {
                _name = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DisplayName));
            }
        }
    }

    [JsonPropertyName("weight")]
    public int Weight
    {
        get => _weight;
        // ReSharper disable once PropertyCanBeMadeInitOnly.Global
        set
        {
            if (_weight != value)
            {
                _weight = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DisplayName));
            }
        }
    }

    [JsonPropertyName("recurring")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Recurring { get; init; }

    [JsonPropertyName("dances")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<DanceItem>? Dances { get; set; }

    [JsonPropertyName("children")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<DanceCategoryNode>? Children { get; set; }

    /// <summary>
    /// Indicates if this is the virtual root node (not serialized).
    /// </summary>
    [JsonIgnore]
    public bool IsRoot { get; init; }

    /// <summary>
    /// Total number of tracks in all dances under this category (recursive).
    /// </summary>
    [JsonIgnore]
    public int TrackCount
    {
        get
        {
            var count = 0;
            if (Dances != null)
            {
                count += Dances.Sum(d => d.TrackCount);
            }
            if (Children != null)
            {
                count += Children.Sum(c => c.TrackCount);
            }
            return count;
        }
    }

    /// <summary>
    /// Whether this category is effectively disabled (weight 0 or no tracks).
    /// </summary>
    [JsonIgnore]
    public bool IsEffectivelyDisabled => Weight == 0 || TrackCount == 0;

    /// <summary>
    /// Display name - name with weight and track count.
    /// </summary>
    [JsonIgnore]
    public string DisplayName => IsRoot ? Name : $"{Name}  Weight: {Weight} ({TrackCount} tracks)";

    private ObservableCollection<object>? _items;

    /// <summary>
    /// Combined collection of children and dances for TreeView binding.
    /// Children come first, then dances.
    /// </summary>
    [JsonIgnore]
    public ObservableCollection<object> Items
    {
        get
        {
            if (_items != null)
                return _items;

            _items = new ObservableCollection<object>();
            if (Children != null)
            {
                foreach (var child in Children)
                {
                    _items.Add(child);
                }
            }
            if (Dances != null)
            {
                foreach (var dance in Dances)
                {
                    _items.Add(dance);
                }
            }
            return _items;
        }
    }

    /// <summary>
    /// Refreshes the Items collection after modifications.
    /// </summary>
    public void RefreshItems()
    {
        _items = null;
        OnPropertyChanged(nameof(Items));
    }

    /// <summary>
    /// Clears all track assignments from dances in this category and all children.
    /// </summary>
    public void ClearAllTrackAssignments()
    {
        if (Dances != null)
        {
            foreach (var dance in Dances)
            {
                dance.ClearAssignedTracks();
            }
        }
        if (Children != null)
        {
            foreach (var child in Children)
            {
                child.ClearAllTrackAssignments();
            }
        }
    }

    /// <summary>
    /// Refreshes display properties related to track counts.
    /// </summary>
    public void RefreshTrackCounts()
    {
        OnPropertyChanged(nameof(TrackCount));
        OnPropertyChanged(nameof(DisplayName));
        OnPropertyChanged(nameof(IsEffectivelyDisabled));

        // Recurse into children
        if (Children != null)
        {
            foreach (var child in Children)
            {
                child.RefreshTrackCounts();
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
