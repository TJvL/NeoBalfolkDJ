using System.Threading.Tasks;
using NeoBalfolkDJ.Models;

namespace NeoBalfolkDJ.Services;

/// <summary>
/// Service that pre-verifies track files are accessible.
/// </summary>
public interface ITrackPreloadService
{
    /// <summary>
    /// Pre-verifies a track file is accessible asynchronously.
    /// </summary>
    /// <param name="track">The track to verify</param>
    /// <returns>True if file is accessible, false otherwise</returns>
    Task<bool> PreloadAsync(Track track);

    /// <summary>
    /// Promotes the "next" preloaded track to "current" when playback starts.
    /// </summary>
    void PromoteNextToCurrent();

    /// <summary>
    /// Clears all preloaded tracks.
    /// </summary>
    void Clear();

    /// <summary>
    /// Checks if a track has been verified accessible.
    /// </summary>
    bool IsCached(Track track);
}


