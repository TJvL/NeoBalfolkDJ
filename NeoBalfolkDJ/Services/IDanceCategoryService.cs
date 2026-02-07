using System.Collections.Generic;
using NeoBalfolkDJ.Models;

namespace NeoBalfolkDJ.Services;

/// <summary>
/// Service for loading, saving, importing, and exporting the dance category tree.
/// </summary>
public interface IDanceCategoryService
{
    /// <summary>
    /// Gets the current loaded categories.
    /// </summary>
    IReadOnlyList<DanceCategoryNode> Categories { get; }

    /// <summary>
    /// Gets the virtual root node that contains all categories.
    /// </summary>
    DanceCategoryNode? GetRootNode();

    /// <summary>
    /// Loads the dance tree from user data, or extracts the default if not present.
    /// </summary>
    List<DanceCategoryNode> Load();

    /// <summary>
    /// Saves the current dance tree to user data.
    /// </summary>
    void Save();

    /// <summary>
    /// Imports a dance tree from a file.
    /// </summary>
    bool Import(string filePath);

    /// <summary>
    /// Exports the current dance tree to a file.
    /// </summary>
    bool Export(string filePath);

    /// <summary>
    /// Resets the dance tree to the embedded default.
    /// </summary>
    void ResetToDefault();
}

