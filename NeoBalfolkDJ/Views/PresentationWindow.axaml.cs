using Avalonia;
using Avalonia.Controls;

namespace NeoBalfolkDJ.Views;

public partial class PresentationWindow : Window
{
    private bool _allowClose;
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
                LastNormalPosition = Position;
                if (Width > 0 && Height > 0)
                {
                    LastNormalSize = new Size(Width, Height);
                }
            }
            _initialized = true;
        };
    }

    private void OnPositionChanged(object? sender, PixelPointEventArgs e)
    {
        // Only track position when in normal state
        if (_initialized && WindowState == WindowState.Normal)
        {
            LastNormalPosition = Position;
        }
    }

    private void OnPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (!_initialized) return;

        if (e.Property == BoundsProperty && WindowState == WindowState.Normal)
        {
            if (Width > 0 && Height > 0)
            {
                LastNormalSize = new Size(Width, Height);
            }
        }

        // When returning to normal state, capture position/size
        if (e.Property == WindowStateProperty && WindowState == WindowState.Normal)
        {
            LastNormalPosition = Position;
            if (Width > 0 && Height > 0)
            {
                LastNormalSize = new Size(Width, Height);
            }
        }
    }

    /// <summary>
    /// Gets the last normal (non-maximized) position
    /// </summary>
    public PixelPoint LastNormalPosition { get; private set; }

    /// <summary>
    /// Gets the last normal (non-maximized) size
    /// </summary>
    public Size LastNormalSize { get; private set; }

    /// <summary>
    /// Sets the initial normal position (used when restoring from saved state)
    /// </summary>
    public void SetLastNormalPosition(PixelPoint position)
    {
        LastNormalPosition = position;
    }

    /// <summary>
    /// Sets the initial normal size (used when restoring from saved state)
    /// </summary>
    public void SetLastNormalSize(Size size)
    {
        LastNormalSize = size;
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
