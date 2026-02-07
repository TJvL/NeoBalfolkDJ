using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeoBalfolkDJ.Models;
using NeoBalfolkDJ.Services;

namespace NeoBalfolkDJ.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    private readonly ILoggingService? _logger;
    private readonly ISettingsService? _settingsService;
    private readonly INotificationService? _notificationService;

    public event EventHandler? BackRequested;
    public event EventHandler<string>? MusicDirectoryChanged;
    public event EventHandler<int>? MaxQueueItemsChanged;
    public event EventHandler<int>? DelaySecondsChanged;
    public event EventHandler<int>? PresentationDisplayCountChanged;
    public event EventHandler<bool>? AutoQueueRandomTrackChanged;
    public event EventHandler<bool>? AllowDuplicateTracksInQueueChanged;
    public event EventHandler<AppTheme>? ThemeChanged;

    /// <summary>
    /// Event raised when import is requested (View handles file picker).
    /// </summary>
    public event EventHandler? ImportSynonymsRequested;

    /// <summary>
    /// Event raised when export is requested (View handles file picker).
    /// </summary>
    public event EventHandler? ExportSynonymsRequested;

    /// <summary>
    /// Event raised when synonym line deletion confirmation is needed.
    /// </summary>
    public event EventHandler<DanceSynonymEntryViewModel>? DeleteSynonymLineRequested;

    /// <summary>
    /// Event raised when log export is requested (View handles file picker).
    /// </summary>
    public event EventHandler? ExportLogRequested;

    [ObservableProperty]
    private string _musicDirectoryPath = string.Empty;

    [ObservableProperty]
    private int _maxQueueItems = 6;

    [ObservableProperty]
    private int _delaySeconds = 30;

    [ObservableProperty]
    private int _presentationDisplayCount;

    [ObservableProperty]
    private bool _autoQueueRandomTrack;

    [ObservableProperty]
    private bool _allowDuplicateTracksInQueue = true;

    [ObservableProperty]
    private AppTheme _selectedTheme = AppTheme.Auto;

    [ObservableProperty]
    private bool _isSynonymEditorVisible;

    [ObservableProperty]
    private DanceSynonymEditorViewModel? _synonymEditor;

    public AppTheme[] AvailableThemes { get; } = [AppTheme.Auto, AppTheme.Light, AppTheme.Dark];

    private IDanceSynonymService? _synonymService;

    /// <summary>
    /// Design-time constructor
    /// </summary>
    public SettingsViewModel() : this(null!, null!)
    {
        if (Design.IsDesignMode)
        {
            MusicDirectoryPath = "/home/user/Music";
            MaxQueueItems = 6;
            DelaySeconds = 30;
        }
    }

    public SettingsViewModel(ILoggingService logger, ISettingsService settingsService, INotificationService? notificationService = null)
    {
        _logger = logger;
        _settingsService = settingsService;
        _notificationService = notificationService;

        if (!Design.IsDesignMode && _settingsService != null)
        {
            LoadSettings();
        }
    }

    public void SetSynonymService(IDanceSynonymService service)
    {
        _synonymService = service;
    }

    [RelayCommand]
    private void Back()
    {
        if (IsSynonymEditorVisible)
        {
            // Go back from synonym editor to settings
            IsSynonymEditorVisible = false;
            SynonymEditor?.Dispose();
            SynonymEditor = null;
        }
        else
        {
            // Go back from settings to main view
            BackRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    [RelayCommand]
    private void EditSynonyms()
    {
        if (_synonymService == null)
        {
            _logger?.Warning("DanceSynonymService not available");
            return;
        }

        SynonymEditor = new DanceSynonymEditorViewModel(_synonymService, _notificationService);
        SynonymEditor.ImportRequested += (_, _) => ImportSynonymsRequested?.Invoke(this, EventArgs.Empty);
        SynonymEditor.ExportRequested += (_, _) => ExportSynonymsRequested?.Invoke(this, EventArgs.Empty);
        SynonymEditor.DeleteLineConfirmationRequested += (_, entry) => DeleteSynonymLineRequested?.Invoke(this, entry);
        IsSynonymEditorVisible = true;
    }

    public void PerformImport(string filePath)
    {
        SynonymEditor?.PerformImport(filePath);
    }

    public void PerformExport(string filePath)
    {
        SynonymEditor?.PerformExport(filePath);
    }

    public void ConfirmDeleteSynonymLine(DanceSynonymEntryViewModel entry)
    {
        SynonymEditor?.ConfirmRemoveLine(entry);
    }

    private void LoadSettings()
    {
        var settings = _settingsService?.Load();
        if (settings == null) return;

        MusicDirectoryPath = settings.MusicDirectoryPath;
        MaxQueueItems = settings.MaxQueueItems;
        DelaySeconds = Math.Clamp(settings.DelaySeconds, 1, 300);
        PresentationDisplayCount = Math.Clamp(settings.PresentationDisplayCount, 0, 6);
        AutoQueueRandomTrack = settings.AutoQueueRandomTrack;
        AllowDuplicateTracksInQueue = settings.AllowDuplicateTracksInQueue;
        SelectedTheme = settings.Theme;
    }

    private void SaveSettings()
    {
        if (_settingsService == null) return;

        // Load existing settings to preserve window state and other properties
        var settings = _settingsService.Load();

        // Update only the settings managed by this view model
        settings.MusicDirectoryPath = MusicDirectoryPath;
        settings.MaxQueueItems = MaxQueueItems;
        settings.DelaySeconds = DelaySeconds;
        settings.PresentationDisplayCount = PresentationDisplayCount;
        settings.AutoQueueRandomTrack = AutoQueueRandomTrack;
        settings.AllowDuplicateTracksInQueue = AllowDuplicateTracksInQueue;
        settings.Theme = SelectedTheme;

        _settingsService.Save(settings);
    }

    partial void OnMusicDirectoryPathChanged(string? oldValue, string newValue)
    {
        SaveSettings();

        // Only fire event if the value actually changed and is not empty
        if (oldValue != newValue && !string.IsNullOrWhiteSpace(newValue))
        {
            MusicDirectoryChanged?.Invoke(this, newValue);
        }
    }

    partial void OnMaxQueueItemsChanged(int oldValue, int newValue)
    {
        SaveSettings();

        if (oldValue != newValue)
        {
            MaxQueueItemsChanged?.Invoke(this, newValue);
        }
    }

    partial void OnDelaySecondsChanged(int oldValue, int newValue)
    {
        // Clamp value between 1 and 300
        if (newValue < 1)
        {
            DelaySeconds = 1;
            return;
        }
        if (newValue > 300)
        {
            DelaySeconds = 300;
            return;
        }

        SaveSettings();

        if (oldValue != newValue)
        {
            DelaySecondsChanged?.Invoke(this, newValue);
        }
    }

    partial void OnPresentationDisplayCountChanged(int oldValue, int newValue)
    {
        // Clamp value between 0 and 6
        if (newValue < 0)
        {
            PresentationDisplayCount = 0;
            return;
        }
        if (newValue > 6)
        {
            PresentationDisplayCount = 6;
            return;
        }

        SaveSettings();

        if (oldValue != newValue)
        {
            PresentationDisplayCountChanged?.Invoke(this, newValue);
        }
    }

    partial void OnAutoQueueRandomTrackChanged(bool oldValue, bool newValue)
    {
        SaveSettings();

        if (oldValue != newValue)
        {
            AutoQueueRandomTrackChanged?.Invoke(this, newValue);
        }
    }

    partial void OnAllowDuplicateTracksInQueueChanged(bool oldValue, bool newValue)
    {
        SaveSettings();

        if (oldValue != newValue)
        {
            AllowDuplicateTracksInQueueChanged?.Invoke(this, newValue);
        }
    }

    partial void OnSelectedThemeChanged(AppTheme oldValue, AppTheme newValue)
    {
        SaveSettings();

        if (oldValue != newValue)
        {
            ThemeChanged?.Invoke(this, newValue);
        }
    }

    [RelayCommand]
    private async Task BrowseForDirectory()
    {
        // This will be called from the view which will handle the folder dialog
        // The view will set MusicDirectoryPath directly after folder selection
        await Task.CompletedTask;
    }

    [RelayCommand]
    private void ExportLog()
    {
        ExportLogRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Gets the log file path for export functionality.
    /// </summary>
    public string? GetLogFilePath()
    {
        return _logger?.LogFilePath;
    }
}
