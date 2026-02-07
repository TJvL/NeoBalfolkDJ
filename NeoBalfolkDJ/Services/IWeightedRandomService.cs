using System;
using System.Collections.Generic;
using NeoBalfolkDJ.Models;

namespace NeoBalfolkDJ.Services;

/// <summary>
/// Service for weighted random track selection from the dance tree.
/// </summary>
public interface IWeightedRandomService
{
    /// <summary>
    /// Assigns all tracks to matching dances in the tree.
    /// </summary>
    void AssignTracksToTree(IReadOnlyList<Track> tracks);

    /// <summary>
    /// Selects a random track from the entire tree using weighted random selection.
    /// </summary>
    /// <param name="excludeFilter">Optional filter to exclude certain tracks</param>
    /// <returns>A randomly selected track, or null if none available</returns>
    Track? SelectRandomTrack(Func<Track, bool>? excludeFilter = null);

    /// <summary>
    /// Selects a random track from a specific branch using weighted random selection.
    /// </summary>
    /// <param name="branch">The branch to select from</param>
    /// <param name="excludeFilter">Optional filter to exclude certain tracks</param>
    /// <returns>A randomly selected track, or null if none available</returns>
    Track? SelectRandomTrackFromBranch(DanceCategoryNode branch, Func<Track, bool>? excludeFilter = null);
}

