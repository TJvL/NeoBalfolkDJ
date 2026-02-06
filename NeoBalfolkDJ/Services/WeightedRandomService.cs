using System;
using System.Collections.Generic;
using System.Linq;
using NeoBalfolkDJ.Models;

namespace NeoBalfolkDJ.Services;

/// <summary>
/// Service for weighted random track selection from the dance tree.
/// Handles track assignment to dances and weighted random selection.
/// </summary>
public class WeightedRandomService
{
    private readonly DanceCategoryService _categoryService;
    private readonly DanceSynonymService _synonymService;
    private readonly NotificationService _notificationService;
    private readonly Random _random = new();
    
    // Cache of normalized dance names to DanceItem for fast lookup
    private Dictionary<string, DanceItem> _normalizedDanceNameLookup = new();
    
    public WeightedRandomService(
        DanceCategoryService categoryService,
        DanceSynonymService synonymService,
        NotificationService notificationService)
    {
        _categoryService = categoryService;
        _synonymService = synonymService;
        _notificationService = notificationService;
    }
    
    /// <summary>
    /// Assigns all tracks to matching dances in the tree.
    /// Uses normalized name matching with synonyms.
    /// </summary>
    public void AssignTracksToTree(IReadOnlyList<Track> tracks)
    {
        var root = _categoryService.GetRootNode();
        if (root == null)
        {
            LoggingService.Warning("Cannot assign tracks: dance tree not loaded");
            return;
        }
        
        // Clear all existing assignments
        root.ClearAllTrackAssignments();
        
        // Build lookup table of normalized dance names to DanceItem
        BuildDanceLookup(root);
        
        var assignedCount = 0;
        var unassignedCount = 0;
        
        foreach (var track in tracks)
        {
            var danceItem = FindDanceForTrack(track);
            if (danceItem != null)
            {
                danceItem.AssignTrack(track);
                assignedCount++;
            }
            else
            {
                unassignedCount++;
            }
        }
        
        // Refresh track counts in the tree
        root.RefreshTrackCounts();
        
        LoggingService.Info($"Track assignment complete: {assignedCount} assigned, {unassignedCount} unassigned");
        
        if (unassignedCount > 0)
        {
            LoggingService.Debug($"{unassignedCount} tracks did not match any dance in the tree");
        }
    }
    
    /// <summary>
    /// Builds a lookup dictionary from normalized dance names (and synonyms) to DanceItem.
    /// </summary>
    private void BuildDanceLookup(DanceCategoryNode root)
    {
        _normalizedDanceNameLookup.Clear();
        
        var allDances = GetAllDances(root);
        
        foreach (var dance in allDances)
        {
            var normalizedName = StringNormalizer.NormalizeForComparison(dance.Name);
            
            // Add the dance's own name
            if (!string.IsNullOrEmpty(normalizedName) && !_normalizedDanceNameLookup.ContainsKey(normalizedName))
            {
                _normalizedDanceNameLookup[normalizedName] = dance;
            }
            
            // Add synonyms for this dance name
            var synonymEntry = FindSynonymEntry(normalizedName);
            if (synonymEntry != null)
            {
                foreach (var synonym in synonymEntry.Synonyms)
                {
                    var normalizedSynonym = StringNormalizer.NormalizeForComparison(synonym);
                    if (!string.IsNullOrEmpty(normalizedSynonym) && !_normalizedDanceNameLookup.ContainsKey(normalizedSynonym))
                    {
                        _normalizedDanceNameLookup[normalizedSynonym] = dance;
                    }
                }
            }
        }
        
        LoggingService.Debug($"Built dance lookup with {_normalizedDanceNameLookup.Count} entries for {allDances.Count} dances");
    }
    
    /// <summary>
    /// Finds the synonym entry that matches the given normalized dance name.
    /// </summary>
    private DanceSynonym? FindSynonymEntry(string normalizedDanceName)
    {
        foreach (var entry in _synonymService.Synonyms)
        {
            if (StringNormalizer.NormalizeForComparison(entry.Name) == normalizedDanceName)
            {
                return entry;
            }
            
            // Also check if the dance name matches any synonym
            foreach (var synonym in entry.Synonyms)
            {
                if (StringNormalizer.NormalizeForComparison(synonym) == normalizedDanceName)
                {
                    return entry;
                }
            }
        }
        return null;
    }
    
    /// <summary>
    /// Finds the DanceItem that matches the track's dance name.
    /// </summary>
    private DanceItem? FindDanceForTrack(Track track)
    {
        if (string.IsNullOrWhiteSpace(track.Dance))
            return null;
        
        var normalizedTrackDance = StringNormalizer.NormalizeForComparison(track.Dance);
        
        // Direct lookup
        if (_normalizedDanceNameLookup.TryGetValue(normalizedTrackDance, out var dance))
        {
            return dance;
        }
        
        return null;
    }
    
    /// <summary>
    /// Gets all DanceItem leaves from the tree recursively.
    /// </summary>
    private List<DanceItem> GetAllDances(DanceCategoryNode node)
    {
        var dances = new List<DanceItem>();
        
        if (node.Dances != null)
        {
            dances.AddRange(node.Dances);
        }
        
        if (node.Children != null)
        {
            foreach (var child in node.Children)
            {
                dances.AddRange(GetAllDances(child));
            }
        }
        
        return dances;
    }
    
    /// <summary>
    /// Selects a random track from the entire tree using weighted random selection.
    /// </summary>
    /// <param name="excludeFilter">Optional filter to exclude certain tracks (e.g., duplicates)</param>
    /// <returns>A randomly selected track, or null if none available</returns>
    public Track? SelectRandomTrack(Func<Track, bool>? excludeFilter = null)
    {
        var root = _categoryService.GetRootNode();
        if (root == null)
        {
            _notificationService.ShowNotification("Dance tree not loaded", NotificationSeverity.Warning);
            return null;
        }
        
        return SelectRandomTrackFromBranch(root, excludeFilter);
    }
    
    /// <summary>
    /// Selects a random track from a specific branch using weighted random selection.
    /// </summary>
    /// <param name="branch">The branch to select from</param>
    /// <param name="excludeFilter">Optional filter to exclude certain tracks</param>
    /// <returns>A randomly selected track, or null if none available</returns>
    public Track? SelectRandomTrackFromBranch(DanceCategoryNode branch, Func<Track, bool>? excludeFilter = null)
    {
        var selectedDance = SelectDanceFromNode(branch, excludeFilter);
        
        if (selectedDance == null)
        {
            _notificationService.ShowNotification("No tracks available for random selection", NotificationSeverity.Warning);
            return null;
        }
        
        // Get available tracks (apply filter)
        var availableTracks = excludeFilter != null
            ? selectedDance.AssignedTracks.Where(t => !excludeFilter(t)).ToList()
            : selectedDance.AssignedTracks.ToList();
        
        if (availableTracks.Count == 0)
        {
            // This shouldn't happen if SelectDanceFromNode works correctly, but handle it
            _notificationService.ShowNotification("No tracks available for random selection", NotificationSeverity.Warning);
            return null;
        }
        
        // Simple random pick from available tracks
        var index = _random.Next(availableTracks.Count);
        return availableTracks[index];
    }
    
    /// <summary>
    /// Performs weighted random selection to choose a dance from the tree.
    /// Skips nodes with weight 0 or no available tracks.
    /// </summary>
    private DanceItem? SelectDanceFromNode(DanceCategoryNode node, Func<Track, bool>? excludeFilter)
    {
        // Collect eligible items (categories and dances with weight > 0 and available tracks)
        var eligibleChildren = new List<(DanceCategoryNode node, int weight)>();
        var eligibleDances = new List<(DanceItem dance, int weight)>();
        
        // Check child categories
        if (node.Children != null)
        {
            foreach (var child in node.Children)
            {
                if (child.Weight > 0 && HasAvailableTracks(child, excludeFilter))
                {
                    eligibleChildren.Add((child, child.Weight));
                }
            }
        }
        
        // Check direct dances
        if (node.Dances != null)
        {
            foreach (var dance in node.Dances)
            {
                if (dance.Weight > 0 && HasAvailableTracks(dance, excludeFilter))
                {
                    eligibleDances.Add((dance, dance.Weight));
                }
            }
        }
        
        // Calculate total weight
        var totalWeight = eligibleChildren.Sum(c => c.weight) + eligibleDances.Sum(d => d.weight);
        
        if (totalWeight == 0)
        {
            return null; // No eligible items
        }
        
        // Random selection
        var randomValue = _random.Next(totalWeight);
        var cumulative = 0;
        
        // Check children first
        foreach (var (child, weight) in eligibleChildren)
        {
            cumulative += weight;
            if (randomValue < cumulative)
            {
                // Recurse into this child
                return SelectDanceFromNode(child, excludeFilter);
            }
        }
        
        // Check dances
        foreach (var (dance, weight) in eligibleDances)
        {
            cumulative += weight;
            if (randomValue < cumulative)
            {
                return dance;
            }
        }
        
        // Should not reach here, but return null just in case
        return null;
    }
    
    /// <summary>
    /// Checks if a category node has any available tracks (after filtering).
    /// </summary>
    private bool HasAvailableTracks(DanceCategoryNode node, Func<Track, bool>? excludeFilter)
    {
        if (node.Dances != null)
        {
            foreach (var dance in node.Dances)
            {
                if (HasAvailableTracks(dance, excludeFilter))
                {
                    return true;
                }
            }
        }
        
        if (node.Children != null)
        {
            foreach (var child in node.Children)
            {
                if (child.Weight > 0 && HasAvailableTracks(child, excludeFilter))
                {
                    return true;
                }
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// Checks if a dance has any available tracks (after filtering).
    /// </summary>
    private bool HasAvailableTracks(DanceItem dance, Func<Track, bool>? excludeFilter)
    {
        if (dance.AssignedTracks.Count == 0)
            return false;
        
        if (excludeFilter == null)
            return true;
        
        return dance.AssignedTracks.Any(t => !excludeFilter(t));
    }
}

