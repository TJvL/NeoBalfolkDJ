using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using NeoBalfolkDJ.Models;
using NeoBalfolkDJ.ViewModels;
using NeoBalfolkDJ.Views;
using WindowState = Avalonia.Controls.WindowState;
using WindowStateSettings = NeoBalfolkDJ.Models.WindowState;

namespace NeoBalfolkDJ.Services;

public class PresentationDisplayService : IDisposable
{
    private readonly List<PresentationWindow> _windows = new();
    private readonly List<PresentationDisplayViewModel> _viewModels = new();
    private readonly Window _owner;
    private bool _disposed;

    // Track current state so new windows can be initialized properly
    private enum DisplayState { Empty, Track, Stop, Delay }
    private DisplayState _currentState = DisplayState.Empty;
    private Track? _currentTrack;
    private IQueueItem? _nextItem;
    private double _currentProgress;
    private double _currentDuration;

    public PresentationDisplayService(Window owner)
    {
        _owner = owner;
    }

    /// <summary>
    /// Updates the number of presentation windows to display
    /// </summary>
    public void UpdateDisplayCount(int count)
    {
        if (_disposed) return;

        count = Math.Clamp(count, 0, 10);

        // Close excess windows
        while (_windows.Count > count)
        {
            var index = _windows.Count - 1;
            var window = _windows[index];
            _windows.RemoveAt(index);
            _viewModels.RemoveAt(index);
            window.ForceClose();
        }

        // Load saved window states
        var settings = SettingsService.Load();

        // Open new windows
        while (_windows.Count < count)
        {
            var windowIndex = _windows.Count;
            var viewModel = new PresentationDisplayViewModel();
            
            // Initialize with current state
            ApplyCurrentState(viewModel);
            
            var window = new PresentationWindow
            {
                DataContext = viewModel,
                Title = $"Presentation Display {windowIndex + 1}"
            };
            
            // Get saved state if available
            WindowStateSettings? savedState = null;
            if (windowIndex < settings.PresentationWindowStates.Count)
            {
                savedState = settings.PresentationWindowStates[windowIndex];
                // Restore size and position before showing
                RestoreWindowSizeAndPosition(window, savedState);
                
                // Set the last normal position/size for tracking (important for maximized windows)
                if (savedState.X.HasValue && savedState.Y.HasValue)
                {
                    window.SetLastNormalPosition(new PixelPoint((int)savedState.X.Value, (int)savedState.Y.Value));
                }
                if (savedState.Width.HasValue && savedState.Height.HasValue)
                {
                    window.SetLastNormalSize(new Size(savedState.Width.Value, savedState.Height.Value));
                }
            }

            _viewModels.Add(viewModel);
            _windows.Add(window);
            window.Show(_owner);
            
            // Restore maximized state after window is shown
            if (savedState?.IsMaximized == true)
            {
                window.WindowState = WindowState.Maximized;
                LoggingService.Debug($"Restored presentation window {windowIndex} to maximized state");
            }
        }

        LoggingService.Debug($"Presentation display count updated to {count}");
    }

    private void RestoreWindowSizeAndPosition(PresentationWindow window, WindowStateSettings state)
    {
        if (state.Width.HasValue && state.Height.HasValue &&
            state.Width.Value > 0 && state.Height.Value > 0)
        {
            window.Width = state.Width.Value;
            window.Height = state.Height.Value;
            LoggingService.Debug($"Restoring presentation window size: {state.Width}x{state.Height}");
        }
        
        if (state.X.HasValue && state.Y.HasValue)
        {
            window.Position = new PixelPoint((int)state.X.Value, (int)state.Y.Value);
            window.WindowStartupLocation = WindowStartupLocation.Manual;
            LoggingService.Debug($"Restoring presentation window position: {state.X},{state.Y}");
        }
    }

    /// <summary>
    /// Saves all presentation window states to settings
    /// </summary>
    public void SaveWindowStates()
    {
        var settings = SettingsService.Load();
        settings.PresentationWindowStates.Clear();
        
        for (int i = 0; i < _windows.Count; i++)
        {
            var window = _windows[i];
            var state = new WindowStateSettings
            {
                IsMaximized = window.WindowState == WindowState.Maximized
            };
            
            if (window.WindowState == WindowState.Normal)
            {
                state.X = window.Position.X;
                state.Y = window.Position.Y;
                state.Width = window.Width;
                state.Height = window.Height;
                LoggingService.Debug($"Saving presentation window {i} normal state: pos=({window.Position.X},{window.Position.Y}), size=({window.Width}x{window.Height})");
            }
            else
            {
                // Use tracked normal position/size for maximized windows
                state.X = window.LastNormalPosition.X;
                state.Y = window.LastNormalPosition.Y;
                state.Width = window.LastNormalSize.Width > 0 ? window.LastNormalSize.Width : 800;
                state.Height = window.LastNormalSize.Height > 0 ? window.LastNormalSize.Height : 600;
                LoggingService.Debug($"Saving presentation window {i} maximized state: lastPos=({window.LastNormalPosition.X},{window.LastNormalPosition.Y}), lastSize=({window.LastNormalSize.Width}x{window.LastNormalSize.Height})");
            }
            
            settings.PresentationWindowStates.Add(state);
        }
        
        SettingsService.Save(settings);
        LoggingService.Debug($"Saved {_windows.Count} presentation window states");
    }

    /// <summary>
    /// Applies the current tracked state to a view model
    /// </summary>
    private void ApplyCurrentState(PresentationDisplayViewModel vm)
    {
        switch (_currentState)
        {
            case DisplayState.Track:
                vm.UpdateCurrentTrack(_currentTrack);
                break;
            case DisplayState.Stop:
                vm.ShowStopMarker();
                break;
            case DisplayState.Delay:
                vm.ShowDelayMarker((long)_currentDuration);
                break;
            case DisplayState.Empty:
            default:
                vm.Clear();
                break;
        }
        
        vm.UpdateNextItem(_nextItem);
        vm.UpdateProgress(_currentProgress, _currentDuration);
    }

    /// <summary>
    /// Updates the current track on all presentation displays
    /// </summary>
    public void UpdateCurrentTrack(Track? track)
    {
        _currentState = track != null ? DisplayState.Track : DisplayState.Empty;
        _currentTrack = track;
        
        foreach (var vm in _viewModels)
        {
            vm.UpdateCurrentTrack(track);
        }
    }

    /// <summary>
    /// Shows stop marker state on all presentation displays
    /// </summary>
    public void ShowStopMarker()
    {
        _currentState = DisplayState.Stop;
        _currentTrack = null;
        
        foreach (var vm in _viewModels)
        {
            vm.ShowStopMarker();
        }
    }

    /// <summary>
    /// Shows delay marker state on all presentation displays
    /// </summary>
    public void ShowDelayMarker(long durationMs)
    {
        _currentState = DisplayState.Delay;
        _currentTrack = null;
        _currentDuration = durationMs;
        _currentProgress = 0;
        
        foreach (var vm in _viewModels)
        {
            vm.ShowDelayMarker(durationMs);
        }
    }

    /// <summary>
    /// Shows message marker state on all presentation displays
    /// </summary>
    public void ShowMessageMarker(string message, long? durationMs)
    {
        _currentState = durationMs.HasValue ? DisplayState.Delay : DisplayState.Stop;
        _currentTrack = null;
        _currentDuration = durationMs ?? 0;
        _currentProgress = 0;
        
        foreach (var vm in _viewModels)
        {
            vm.ShowMessageMarker(message, durationMs);
        }
    }

    /// <summary>
    /// Updates the next item on all presentation displays
    /// </summary>
    public void UpdateNextItem(IQueueItem? item)
    {
        _nextItem = item;
        
        foreach (var vm in _viewModels)
        {
            vm.UpdateNextItem(item);
        }
    }

    /// <summary>
    /// Updates the progress bar on all presentation displays
    /// </summary>
    public void UpdateProgress(double progress, double duration)
    {
        _currentProgress = progress;
        _currentDuration = duration;
        
        foreach (var vm in _viewModels)
        {
            vm.UpdateProgress(progress, duration);
        }
    }

    /// <summary>
    /// Clears all presentation displays
    /// </summary>
    public void Clear()
    {
        _currentState = DisplayState.Empty;
        _currentTrack = null;
        _nextItem = null;
        _currentProgress = 0;
        _currentDuration = 0;
        
        foreach (var vm in _viewModels)
        {
            vm.Clear();
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        // Save window states before closing
        SaveWindowStates();

        foreach (var window in _windows)
        {
            window.ForceClose();
        }
        _windows.Clear();
        _viewModels.Clear();
    }
}
