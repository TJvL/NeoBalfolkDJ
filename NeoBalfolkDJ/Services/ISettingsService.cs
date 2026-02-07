using NeoBalfolkDJ.Models;

namespace NeoBalfolkDJ.Services;

/// <summary>
/// Service for loading and saving application settings.
/// </summary>
public interface ISettingsService
{
    /// <summary>
    /// Loads settings from storage. Returns default settings if not found or on error.
    /// </summary>
    ApplicationSettings Load();

    /// <summary>
    /// Saves settings to storage.
    /// </summary>
    void Save(ApplicationSettings settings);
}

