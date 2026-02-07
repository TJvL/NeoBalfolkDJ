using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using NeoBalfolkDJ.Models;

namespace NeoBalfolkDJ.Services;

/// <summary>
/// Service for loading, saving, importing, and exporting the dance category tree.
/// Stores the tree in user local application data with a default from embedded resources.
/// </summary>
public sealed class DanceCategoryService(ILoggingService logger, INotificationService? notificationService = null)
    : IDanceCategoryService, IDisposable
{
    private static readonly string DataDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "NeoBalfolkDJ");

    private static readonly string DanceTreeFilePath = Path.Combine(DataDirectory, "dancetree.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private static readonly JsonSerializerOptions StrictJsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };

    private readonly ILoggingService _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    private List<DanceCategoryNode> _categories = new();
    private DanceCategoryNode? _virtualRoot;
    private bool _disposed;

    /// <summary>
    /// Gets the current loaded categories.
    /// </summary>
    public IReadOnlyList<DanceCategoryNode> Categories => _categories;

    /// <summary>
    /// Gets the virtual root node that contains all categories.
    /// </summary>
    public DanceCategoryNode? GetRootNode()
    {
        if (_virtualRoot == null && _categories.Count > 0)
        {
            _virtualRoot = new DanceCategoryNode
            {
                Name = "Dance",
                IsRoot = true,
                Weight = 1, // Root always has weight 1
                Children = _categories
            };
        }
        return _virtualRoot;
    }

    /// <summary>
    /// Loads the dance tree from user data, or extracts the default if not present.
    /// </summary>
    public List<DanceCategoryNode> Load()
    {
        try
        {
            // Ensure directory exists
            Directory.CreateDirectory(DataDirectory);

            // If user file doesn't exist, extract default from embedded resource
            if (!File.Exists(DanceTreeFilePath))
            {
                _logger.Info("Dance tree file not found, extracting default");
                ExtractDefaultDanceTree();
            }

            // Load from user file
            if (File.Exists(DanceTreeFilePath))
            {
                var json = File.ReadAllText(DanceTreeFilePath);
                var categories = JsonSerializer.Deserialize<List<DanceCategoryNode>>(json, JsonOptions);
                if (categories != null)
                {
                    _categories = categories;
                    _virtualRoot = null; // Invalidate virtual root
                    _logger.Info($"Loaded dance tree with {_categories.Count} top-level categories");
                    return _categories;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to load dance tree", ex);
            notificationService?.ShowNotification("Failed to load dance tree: " + ex.Message, NotificationSeverity.Error);
        }

        _categories = new List<DanceCategoryNode>();
        _virtualRoot = null; // Invalidate virtual root
        return _categories;
    }

    /// <summary>
    /// Saves the current categories to user data.
    /// </summary>
    public void Save()
    {
        try
        {
            Directory.CreateDirectory(DataDirectory);
            var json = JsonSerializer.Serialize(_categories, JsonOptions);
            File.WriteAllText(DanceTreeFilePath, json);
            _logger.Info("Dance tree saved successfully");
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to save dance tree", ex);
            notificationService?.ShowNotification("Failed to save dance tree: " + ex.Message, NotificationSeverity.Error);
        }
    }

    /// <summary>
    /// Imports a dance tree from the specified file path with strict JSON validation.
    /// </summary>
    /// <param name="filePath">Path to the JSON file to import</param>
    /// <returns>True if import was successful, false otherwise</returns>
    public bool Import(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                var message = $"Import file not found: {filePath}";
                _logger.Warning(message);
                notificationService?.ShowNotification(message, NotificationSeverity.Warning);
                return false;
            }

            var json = File.ReadAllText(filePath);

            // Use strict deserialization to validate structure
            var categories = JsonSerializer.Deserialize<List<DanceCategoryNode>>(json, StrictJsonOptions);

            if (categories == null)
            {
                var message = "Import failed: JSON file contains null or invalid root structure";
                _logger.Warning(message);
                notificationService?.ShowNotification(message, NotificationSeverity.Warning);
                return false;
            }

            // Validate the structure recursively
            ValidateCategoryStructure(categories);

            // If validation passed, update and save
            _categories = categories;
            Save();

            var successMessage = $"Successfully imported dance tree with {_categories.Count} top-level categories";
            _logger.Info(successMessage);
            notificationService?.ShowNotification(successMessage, NotificationSeverity.Information);
            return true;
        }
        catch (JsonException ex)
        {
            var message = $"Import failed: Invalid JSON structure - {ex.Message}";
            _logger.Error(message, ex);
            notificationService?.ShowNotification(message, NotificationSeverity.Error);
            return false;
        }
        catch (InvalidOperationException ex)
        {
            var message = $"Import failed: {ex.Message}";
            _logger.Error(message, ex);
            notificationService?.ShowNotification(message, NotificationSeverity.Error);
            return false;
        }
        catch (Exception ex)
        {
            var message = $"Import failed: {ex.Message}";
            _logger.Error(message, ex);
            notificationService?.ShowNotification(message, NotificationSeverity.Error);
            return false;
        }
    }

    /// <summary>
    /// Exports the current dance tree to the specified file path.
    /// </summary>
    /// <param name="filePath">Path to export the JSON file to</param>
    /// <returns>True if export was successful, false otherwise</returns>
    public bool Export(string filePath)
    {
        try
        {
            var json = JsonSerializer.Serialize(_categories, JsonOptions);
            File.WriteAllText(filePath, json);

            var successMessage = $"Successfully exported dance tree to {Path.GetFileName(filePath)}";
            _logger.Info(successMessage);
            notificationService?.ShowNotification(successMessage, NotificationSeverity.Information);
            return true;
        }
        catch (Exception ex)
        {
            var message = $"Export failed: {ex.Message}";
            _logger.Error(message, ex);
            notificationService?.ShowNotification(message, NotificationSeverity.Error);
            return false;
        }
    }

    /// <summary>
    /// Resets the dance tree to the embedded default.
    /// </summary>
    public void ResetToDefault()
    {
        try
        {
            if (File.Exists(DanceTreeFilePath))
            {
                File.Delete(DanceTreeFilePath);
            }
            ExtractDefaultDanceTree();
            Load();
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to reset dance tree to default", ex);
            notificationService?.ShowNotification("Failed to reset dance tree: " + ex.Message, NotificationSeverity.Error);
        }
    }

    /// <summary>
    /// Validates the structure of the imported categories recursively.
    /// </summary>
    private void ValidateCategoryStructure(List<DanceCategoryNode> categories)
    {
        foreach (var category in categories)
        {
            if (string.IsNullOrWhiteSpace(category.Name))
            {
                throw new InvalidOperationException("Category must have a non-empty name");
            }

            // Validate dances if present
            if (category.Dances != null)
            {
                foreach (var dance in category.Dances)
                {
                    if (string.IsNullOrWhiteSpace(dance.Name))
                    {
                        throw new InvalidOperationException($"Dance in category '{category.Name}' must have a non-empty name");
                    }
                }
            }

            // Recursively validate children
            if (category.Children != null)
            {
                ValidateCategoryStructure(category.Children);
            }
        }
    }

    /// <summary>
    /// Extracts the default dance tree from embedded resources.
    /// </summary>
    private void ExtractDefaultDanceTree()
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "NeoBalfolkDJ.Assets.dancetree.json";

            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                _logger.Error($"Embedded resource '{resourceName}' not found");
                return;
            }

            using var reader = new StreamReader(stream);
            var json = reader.ReadToEnd();

            File.WriteAllText(DanceTreeFilePath, json);
            _logger.Info("Default dance tree extracted successfully");
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to extract default dance tree", ex);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;
    }
}
