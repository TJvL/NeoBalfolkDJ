using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using Microsoft.Extensions.DependencyInjection;
using NeoBalfolkDJ.Helpers;
using NeoBalfolkDJ.Models;
using NeoBalfolkDJ.Services;
using NeoBalfolkDJ.ViewModels;

namespace NeoBalfolkDJ.Views;

public partial class MainWindow : Window
{
    private PresentationDisplayService? _presentationService;
    private bool _allowClose;
    private PixelPoint _lastNormalPosition;
    private Size _lastNormalSize;

    private readonly ILoggingService? _logger;
    private readonly ISettingsService? _settingsService;

    public MainWindow()
    {
        InitializeComponent();

        // Get services from DI
        _logger = App.Services.GetService<ILoggingService>();
        _settingsService = App.Services.GetService<ISettingsService>();

        // Initialize presentation service after window is loaded
        Opened += OnOpened;
        Closing += OnClosing;

        // Track position and size changes for saving
        PositionChanged += OnPositionChanged;
        PropertyChanged += OnPropertyChanged;

        // Handle keyboard shortcuts
        KeyDown += OnKeyDown;
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel)
            return;

        // Handle Ctrl+Z (Undo)
        if (e is { Key: Key.Z, KeyModifiers: KeyModifiers.Control })
        {
            // If synonym editor is visible, undo there
            if (viewModel is { IsSettingsVisible: true, Settings.IsSynonymEditorVisible: true })
            {
                if (viewModel.Settings.SynonymEditor?.CanUndo == true)
                {
                    viewModel.Settings.SynonymEditor.UndoCommand.Execute(null);
                    e.Handled = true;
                }
            }
            // If dance tree is visible, undo there
            else if (viewModel is { IsSettingsVisible: false, IsHelpVisible: false, TrackList: { IsTreeViewMode: true, CanUndo: true } })
            {
                viewModel.TrackList.UndoCommand.Execute(null);
                e.Handled = true;
            }
        }
        // Handle Ctrl+Shift+Z (Redo)
        else if (e is { Key: Key.Z, KeyModifiers: (KeyModifiers.Control | KeyModifiers.Shift) })
        {
            // If synonym editor is visible, redo there
            if (viewModel is { IsSettingsVisible: true, Settings.IsSynonymEditorVisible: true })
            {
                if (viewModel.Settings.SynonymEditor?.CanRedo == true)
                {
                    viewModel.Settings.SynonymEditor.RedoCommand.Execute(null);
                    e.Handled = true;
                }
            }
            // If dance tree is visible, redo there
            else if (viewModel is { IsSettingsVisible: false, IsHelpVisible: false, TrackList: { IsTreeViewMode: true, CanRedo: true } })
            {
                viewModel.TrackList.RedoCommand.Execute(null);
                e.Handled = true;
            }
        }
        // Handle Delete key for queue items (not AutoQueuedTrack)
        else if (e is { Key: Key.Delete, KeyModifiers: KeyModifiers.None })
        {
            // Only when main view is visible (not settings, not help) and not in history mode
            if (viewModel is { IsSettingsVisible: false, IsHelpVisible: false, Queue.IsHistoryMode: false })
            {
                // Check if there's a selected item that's not an AutoQueuedTrack
                if (viewModel.Queue.SelectedItem != null && viewModel.Queue.SelectedItem is not AutoQueuedTrack)
                {
                    viewModel.Queue.RemoveSelectedItem();
                    e.Handled = true;
                }
            }
        }
    }

    private void OnPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        // Track size changes when in normal state
        if (e.Property == BoundsProperty && WindowState == Avalonia.Controls.WindowState.Normal)
        {
            if (Width > 0 && Height > 0)
            {
                _lastNormalSize = new Size(Width, Height);
            }
        }

        // Track window state changes - capture position BEFORE going to maximize
        if (e.Property == WindowStateProperty)
        {
            if (WindowState == Avalonia.Controls.WindowState.Normal)
            {
                // Returning to normal - capture new position/size
                _lastNormalPosition = Position;
                if (Width > 0 && Height > 0)
                {
                    _lastNormalSize = new Size(Width, Height);
                }
            }
        }
    }

    private void OnPositionChanged(object? sender, PixelPointEventArgs e)
    {
        // Only track position when in normal state
        if (WindowState == Avalonia.Controls.WindowState.Normal)
        {
            _lastNormalPosition = Position;
        }
    }

    private void RestoreWindowState()
    {
        if (_settingsService == null) return;

        var settings = _settingsService.Load();

        // Check if we have saved state
        var hasPosition = settings is { WindowX: not null, WindowY: not null } &&
                          (settings.WindowX.Value != 0 || settings.WindowY.Value != 0 || !settings.IsMaximized);
        var hasSize = settings is { WindowWidth: > 0, WindowHeight: > 0 };

        _logger?.Debug($"RestoreWindowState: hasPosition={hasPosition} ({settings.WindowX},{settings.WindowY}), hasSize={hasSize}, isMaximized={settings.IsMaximized}");

        // Restore size immediately
        if (hasSize)
        {
            Width = settings.WindowWidth!.Value;
            Height = settings.WindowHeight!.Value;
            _lastNormalSize = new Size(settings.WindowWidth.Value, settings.WindowHeight.Value);
        }
        else
        {
            _lastNormalSize = new Size(Width, Height);
        }

        // Restore position
        if (hasPosition)
        {
            WindowStartupLocation = WindowStartupLocation.Manual;
            Position = new PixelPoint((int)settings.WindowX!.Value, (int)settings.WindowY!.Value);
            _lastNormalPosition = Position;
            _logger?.Debug($"Restored window position: {settings.WindowX},{settings.WindowY}");
        }
        else
        {
            _lastNormalPosition = Position;
        }

        // Restore maximized state after a short delay to ensure position is applied first
        if (settings.IsMaximized)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                WindowState = Avalonia.Controls.WindowState.Maximized;
                _logger?.Debug("Restored window state: Maximized");
            }, Avalonia.Threading.DispatcherPriority.Background);
        }
    }

    private void SaveWindowState()
    {
        if (_settingsService == null) return;

        var settings = _settingsService.Load();

        // Save the normal (non-maximized) position and size
        if (WindowState == Avalonia.Controls.WindowState.Normal)
        {
            settings.WindowX = Position.X;
            settings.WindowY = Position.Y;
            settings.WindowWidth = Width;
            settings.WindowHeight = Height;
            _logger?.Debug($"Saving normal window state: pos=({Position.X},{Position.Y}), size=({Width}x{Height})");
        }
        else
        {
            // Use last known normal position/size
            settings.WindowX = _lastNormalPosition.X;
            settings.WindowY = _lastNormalPosition.Y;
            settings.WindowWidth = _lastNormalSize.Width > 0 ? _lastNormalSize.Width : Width;
            settings.WindowHeight = _lastNormalSize.Height > 0 ? _lastNormalSize.Height : Height;
            _logger?.Debug($"Saving maximized window state: lastPos=({_lastNormalPosition.X},{_lastNormalPosition.Y}), lastSize=({_lastNormalSize.Width}x{_lastNormalSize.Height})");
        }

        settings.IsMaximized = WindowState == Avalonia.Controls.WindowState.Maximized;

        _settingsService.Save(settings);
        _logger?.Debug($"Window state saved. IsMaximized={settings.IsMaximized}");
    }

    private void OnOpened(object? sender, System.EventArgs e)
    {
        // Restore window state after window is opened (better timing for multi-monitor setups)
        RestoreWindowState();

        if (DataContext is MainWindowViewModel viewModel && _logger != null && _settingsService != null)
        {
            _presentationService = new PresentationDisplayService(_logger, _settingsService, this);
            viewModel.SetPresentationService(_presentationService);
            viewModel.ExitRequested += OnExitRequested;
            viewModel.ExportHistoryRequested += OnExportHistoryRequested;
        }
    }

    private void OnExportHistoryRequested(object? sender, System.EventArgs e)
    {
        AsyncHelper.SafeFireAndForget(async () =>
        {
            if (DataContext is not MainWindowViewModel viewModel)
                return;

            var storageProvider = StorageProvider;

            var file = await storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Export History",
                SuggestedFileName = "track_history.json",
                FileTypeChoices = new List<FilePickerFileType>
                {
                    new("JSON Files") { Patterns = ["*.json"] },
                    new("All Files") { Patterns = ["*"] }
                }
            });

            if (file != null)
            {
                await viewModel.ExportHistoryAsync(file.Path.LocalPath);
            }
        }, userFriendlyError: "Failed to export history");
    }

    private void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        // Prevent closing unless explicitly allowed through exit button
        if (!_allowClose)
        {
            e.Cancel = true;
            return;
        }

        // Save window state before closing
        SaveWindowState();
    }

    private void OnExitRequested(object? sender, System.EventArgs e)
    {
        _allowClose = true;

        // Save main window state before closing
        SaveWindowState();

        // Dispose presentation windows (this saves their states and closes them)
        _presentationService?.Dispose();
        _presentationService = null;

        // Dispose the PlaybackViewModel to stop audio playback
        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.Playback.Dispose();
        }

        // Close the main window
        Close();

        // Shutdown the application
        if (Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }
}
