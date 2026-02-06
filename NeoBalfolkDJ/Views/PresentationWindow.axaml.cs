using Avalonia;
using Avalonia.Controls;
using NeoBalfolkDJ.Services;

namespace NeoBalfolkDJ.Views;

public partial class PresentationWindow : Window
{
    private bool _allowClose;
    private PixelPoint _lastNormalPosition;
    private Size _lastNormalSize;
    private bool _initialized;

    public PresentationWindow()
    {
        InitializeComponent();
        
        PositionChanged += OnPositionChanged;
        PropertyChanged += OnPropertyChanged;
        
        Opened += (_, _) =>
        {
            // Capture initial state after window is fully opened
            if (WindowState == WindowState.Normal)
            {
                _lastNormalPosition = Position;
                if (Width > 0 && Height > 0)
                {
                    _lastNormalSize = new Size(Width, Height);
                }
            }
            _initialized = true;
            LoggingService.Debug($"PresentationWindow opened: pos=({Position.X},{Position.Y}), size=({Width}x{Height}), state={WindowState}");
        };
    }

    private void OnPositionChanged(object? sender, PixelPointEventArgs e)
    {
        // Only track position when in normal state
        if (_initialized && WindowState == WindowState.Normal)
        {
            _lastNormalPosition = Position;
            LoggingService.Debug($"PresentationWindow position changed: ({Position.X},{Position.Y})");
        }
    }

    private void OnPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (!_initialized) return;
        
        if (e.Property == BoundsProperty && WindowState == WindowState.Normal)
        {
            if (Width > 0 && Height > 0)
            {
                _lastNormalSize = new Size(Width, Height);
            }
        }
        
        // When returning to normal state, capture position/size
        if (e.Property == WindowStateProperty && WindowState == WindowState.Normal)
        {
            _lastNormalPosition = Position;
            if (Width > 0 && Height > 0)
            {
                _lastNormalSize = new Size(Width, Height);
            }
        }
    }

    /// <summary>
    /// Gets the last normal (non-maximized) position
    /// </summary>
    public PixelPoint LastNormalPosition => _lastNormalPosition;

    /// <summary>
    /// Gets the last normal (non-maximized) size
    /// </summary>
    public Size LastNormalSize => _lastNormalSize;

    /// <summary>
    /// Sets the initial normal position (used when restoring from saved state)
    /// </summary>
    public void SetLastNormalPosition(PixelPoint position)
    {
        _lastNormalPosition = position;
    }

    /// <summary>
    /// Sets the initial normal size (used when restoring from saved state)
    /// </summary>
    public void SetLastNormalSize(Size size)
    {
        _lastNormalSize = size;
    }

    /// <summary>
    /// Override closing to hide the window instead of closing when user tries to close it
    /// </summary>
    protected override void OnClosing(WindowClosingEventArgs e)
    {
        // Only allow closing through the service (not by user action)
        if (!_allowClose)
        {
            e.Cancel = true;
        }
        base.OnClosing(e);
    }


    /// <summary>
    /// Forces the window to close, bypassing the closing prevention
    /// </summary>
    public void ForceClose()
    {
        _allowClose = true;
        Close();
    }
}
