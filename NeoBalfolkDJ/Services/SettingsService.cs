using System;
using System.IO;
using System.Text.Json;
using NeoBalfolkDJ.Models;

namespace NeoBalfolkDJ.Services;

/// <summary>
/// File-based settings service implementation.
/// </summary>
public sealed class SettingsService : ISettingsService, IDisposable
{
    private readonly ILoggingService _logger;
    private readonly string _settingsDirectory;
    private readonly string _settingsFilePath;
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };
    private bool _disposed;

    public SettingsService(ILoggingService logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _settingsDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "NeoBalfolkDJ");
        _settingsFilePath = Path.Combine(_settingsDirectory, "settings.json");
    }

    public ApplicationSettings Load()
    {
        if (_disposed) return new ApplicationSettings();

        try
        {
            if (File.Exists(_settingsFilePath))
            {
                var json = File.ReadAllText(_settingsFilePath);
                _logger.Debug($"Settings loaded from {_settingsFilePath}");
                return JsonSerializer.Deserialize<ApplicationSettings>(json, _jsonOptions) ?? new ApplicationSettings();
            }
            _logger.Debug("No settings file found, using defaults");
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to load settings", ex);
        }

        return new ApplicationSettings();
    }

    public void Save(ApplicationSettings settings)
    {
        if (_disposed) return;

        try
        {
            Directory.CreateDirectory(_settingsDirectory);
            var json = JsonSerializer.Serialize(settings, _jsonOptions);
            File.WriteAllText(_settingsFilePath, json);
            _logger.Debug("Settings saved successfully");
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to save settings", ex);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
    }
}

