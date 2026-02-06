using System;
using System.IO;
using System.Text.Json;
using NeoBalfolkDJ.Models;

namespace NeoBalfolkDJ.Services;

public static class SettingsService
{
    private static readonly string SettingsDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "NeoBalfolkDJ");
    
    private static readonly string SettingsFilePath = Path.Combine(SettingsDirectory, "settings.json");
    
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    
    public static ApplicationSettings Load()
    {
        try
        {
            if (File.Exists(SettingsFilePath))
            {
                var json = File.ReadAllText(SettingsFilePath);
                LoggingService.Debug($"Settings loaded from {SettingsFilePath}");
                return JsonSerializer.Deserialize<ApplicationSettings>(json) ?? new ApplicationSettings();
            }
            LoggingService.Debug("No settings file found, using defaults");
        }
        catch (Exception ex)
        {
            LoggingService.Error("Failed to load settings", ex);
        }
        
        return new ApplicationSettings();
    }
    
    public static void Save(ApplicationSettings settings)
    {
        try
        {
            Directory.CreateDirectory(SettingsDirectory);
            var json = JsonSerializer.Serialize(settings, JsonOptions);
            File.WriteAllText(SettingsFilePath, json);
            LoggingService.Debug("Settings saved successfully");
        }
        catch (Exception ex)
        {
            LoggingService.Error("Failed to save settings", ex);
        }
    }
}

