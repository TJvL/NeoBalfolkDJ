using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using NeoBalfolkDJ.Helpers;
using NeoBalfolkDJ.ViewModels;

namespace NeoBalfolkDJ.Views;

public partial class PlaybackView : UserControl
{
    private CancellationTokenSource? _marqueeAnimationCts;
    private CancellationTokenSource? _messageAnimationCts;
    private CancellationTokenSource? _restartDebounce;
    private CancellationTokenSource? _messageRestartDebounce;
    private bool _isAnimating;
    private bool _isMessageAnimating;
    private double _lastPanelWidth;
    private double _lastCanvasWidth;
    private double _lastMessageTextWidth;
    private double _lastMessageCanvasWidth;
    private PlaybackViewModel? _subscribedViewModel;

    public PlaybackView()
    {
        InitializeComponent();

        // Use Loaded event for initial setup
        Loaded += OnLoaded;

        // Watch for data context changes (track changes)
        DataContextChanged += OnDataContextChanged;
    }

    private void OnLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        // Start monitoring after loaded
        TrackInfoPanel.PropertyChanged += OnTrackInfoPanelPropertyChanged;
        TrackInfoCanvas.PropertyChanged += OnTrackInfoCanvasPropertyChanged;
        MessageTextBlock.PropertyChanged += OnMessageTextBlockPropertyChanged;
        MessageCanvas.PropertyChanged += OnMessageCanvasPropertyChanged;

        // Subscribe to confirmation events (only if not already subscribed via DataContextChanged)
        SubscribeToViewModel(DataContext as PlaybackViewModel);

        // Initial animation start
        ScheduleMarqueeRestart();
        ScheduleMessageMarqueeRestart();
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        // Re-subscribe to confirmation events when DataContext changes
        SubscribeToViewModel(DataContext as PlaybackViewModel);

        ScheduleMarqueeRestart();
        ScheduleMessageMarqueeRestart();
    }

    private void SubscribeToViewModel(PlaybackViewModel? viewModel)
    {
        // Unsubscribe from previous view model if any
        if (_subscribedViewModel != null)
        {
            _subscribedViewModel.RestartConfirmationRequested -= OnRestartConfirmationRequested;
            _subscribedViewModel.SkipConfirmationRequested -= OnSkipConfirmationRequested;
            _subscribedViewModel.ClearConfirmationRequested -= OnClearConfirmationRequested;
            _subscribedViewModel = null;
        }

        // Subscribe to new view model
        if (viewModel != null)
        {
            viewModel.RestartConfirmationRequested += OnRestartConfirmationRequested;
            viewModel.SkipConfirmationRequested += OnSkipConfirmationRequested;
            viewModel.ClearConfirmationRequested += OnClearConfirmationRequested;
            _subscribedViewModel = viewModel;
        }
    }

    private void OnRestartConfirmationRequested(object? sender, EventArgs e)
    {
        AsyncHelper.SafeFireAndForget(async () =>
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return;

            var dialog = new ConfirmationDialog();
            dialog.Setup("Restart Track", "Are you sure you want to restart the current track?");

            await dialog.ShowDialog((Window)topLevel);

            if (DataContext is PlaybackViewModel viewModel)
            {
                if (dialog.IsConfirmed)
                {
                    await viewModel.ConfirmRestartAsync();
                }
                else
                {
                    viewModel.CancelRestart();
                }
            }
        });
    }

    private void OnSkipConfirmationRequested(object? sender, EventArgs e)
    {
        AsyncHelper.SafeFireAndForget(async () =>
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return;

            var dialog = new ConfirmationDialog();
            dialog.Setup("Skip to Next", "Are you sure you want to skip to the next item in the queue?");

            await dialog.ShowDialog((Window)topLevel);

            if (DataContext is PlaybackViewModel viewModel)
            {
                if (dialog.IsConfirmed)
                {
                    viewModel.ConfirmSkip();
                }
                else
                {
                    viewModel.CancelSkip();
                }
            }
        });
    }

    private void OnClearConfirmationRequested(object? sender, EventArgs e)
    {
        AsyncHelper.SafeFireAndForget(async () =>
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return;

            var dialog = new ConfirmationDialog();
            dialog.Setup("Clear Track", "Are you sure you want to clear the current track?");

            await dialog.ShowDialog((Window)topLevel);

            if (DataContext is PlaybackViewModel viewModel)
            {
                if (dialog.IsConfirmed)
                {
                    viewModel.ConfirmClear();
                }
                else
                {
                    viewModel.CancelClear();
                }
            }
        });
    }

    private void OnTrackInfoPanelPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property.Name == "Bounds")
        {
            var newWidth = TrackInfoPanel.Bounds.Width;
            if (Math.Abs(newWidth - _lastPanelWidth) > 1)
            {
                _lastPanelWidth = newWidth;
                ScheduleMarqueeRestart();
            }
        }
    }

    private void OnTrackInfoCanvasPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property.Name == "Bounds")
        {
            var newWidth = TrackInfoCanvas.Bounds.Width;
            if (Math.Abs(newWidth - _lastCanvasWidth) > 1)
            {
                _lastCanvasWidth = newWidth;
                ScheduleMarqueeRestart();
            }
        }
    }

    private void OnMessageTextBlockPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property.Name == "Bounds")
        {
            var newWidth = MessageTextBlock.Bounds.Width;
            if (Math.Abs(newWidth - _lastMessageTextWidth) > 1)
            {
                _lastMessageTextWidth = newWidth;
                ScheduleMessageMarqueeRestart();
            }
        }
    }

    private void OnMessageCanvasPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property.Name == "Bounds")
        {
            var newWidth = MessageCanvas.Bounds.Width;
            if (Math.Abs(newWidth - _lastMessageCanvasWidth) > 1)
            {
                _lastMessageCanvasWidth = newWidth;
                ScheduleMessageMarqueeRestart();
            }
        }
    }

    private void ScheduleMarqueeRestart()
    {
        _restartDebounce?.Cancel();
        _restartDebounce = new CancellationTokenSource();
        var token = _restartDebounce.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(100, token);
                if (!token.IsCancellationRequested)
                {
                    await Dispatcher.UIThread.InvokeAsync(RestartMarqueeAnimation);
                }
            }
            catch (OperationCanceledException) { }
        }, token);
    }

    private void RestartMarqueeAnimation()
    {
        // Cancel any existing animation
        _marqueeAnimationCts?.Cancel();
        _marqueeAnimationCts = new CancellationTokenSource();

        // Reset position and center vertically
        Canvas.SetLeft(TrackInfoPanel, 0);
        CenterPanelVertically();

        // Start new animation if needed
        _isAnimating = false;
        _ = StartMarqueeAnimationAsync(_marqueeAnimationCts.Token);
    }

    private void CenterPanelVertically()
    {
        var canvasHeight = TrackInfoCanvas.Bounds.Height;
        var panelHeight = TrackInfoPanel.Bounds.Height;

        if (canvasHeight > 0 && panelHeight > 0)
        {
            var top = (canvasHeight - panelHeight) / 2;
            Canvas.SetTop(TrackInfoPanel, top);
        }
    }

    private async Task StartMarqueeAnimationAsync(CancellationToken token)
    {
        if (_isAnimating) return;

        try
        {
            _isAnimating = true;

            // Wait for layout to settle
            await Task.Delay(300, token);

            // Ensure vertical centering
            await Dispatcher.UIThread.InvokeAsync(CenterPanelVertically);

            while (!token.IsCancellationRequested)
            {
                double canvasWidth = 0;
                double panelWidth = 0;

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    canvasWidth = TrackInfoCanvas.Bounds.Width;
                    panelWidth = TrackInfoPanel.Bounds.Width;
                });

                // Only scroll if text is wider than container
                if (panelWidth > canvasWidth && canvasWidth > 0)
                {
                    var scrollDistance = panelWidth - canvasWidth + 20; // Extra padding
                    var animationDuration = TimeSpan.FromMilliseconds(scrollDistance * 40); // Speed: 40ms per pixel

                    // Wait at start
                    await Task.Delay(2000, token);

                    // Scroll left
                    await AnimateCanvasLeftAsync(TrackInfoPanel, 0, -scrollDistance, animationDuration, token);

                    // Wait at end
                    await Task.Delay(2000, token);

                    // Reset to start
                    await Dispatcher.UIThread.InvokeAsync(() => Canvas.SetLeft(TrackInfoPanel, 0));

                    // Wait before repeating
                    await Task.Delay(1000, token);
                }
                else
                {
                    // Center the text if it fits
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        if (canvasWidth > 0 && panelWidth > 0 && panelWidth <= canvasWidth)
                        {
                            Canvas.SetLeft(TrackInfoPanel, (canvasWidth - panelWidth) / 2);
                        }
                    });

                    // Check again after a delay (in case text changes)
                    await Task.Delay(2000, token);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Animation was canceled, that's fine
        }
        finally
        {
            _isAnimating = false;
        }
    }

    private async Task AnimateCanvasLeftAsync(Control target, double from, double to, TimeSpan duration, CancellationToken token)
    {
        const int frameRate = 60;
        var frameTime = TimeSpan.FromMilliseconds(1000.0 / frameRate);
        var totalFrames = (int)(duration.TotalMilliseconds / frameTime.TotalMilliseconds);

        if (totalFrames <= 0) totalFrames = 1;

        for (int i = 0; i <= totalFrames && !token.IsCancellationRequested; i++)
        {
            var progress = (double)i / totalFrames;
            var currentValue = from + (to - from) * progress;

            await Dispatcher.UIThread.InvokeAsync(() => Canvas.SetLeft(target, currentValue));

            try
            {
                await Task.Delay(frameTime, token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    // Message marquee animation methods

    private void ScheduleMessageMarqueeRestart()
    {
        _messageRestartDebounce?.Cancel();
        _messageRestartDebounce = new CancellationTokenSource();
        var token = _messageRestartDebounce.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(100, token);
                if (!token.IsCancellationRequested)
                {
                    await Dispatcher.UIThread.InvokeAsync(RestartMessageMarqueeAnimation);
                }
            }
            catch (OperationCanceledException) { }
        }, token);
    }

    private void RestartMessageMarqueeAnimation()
    {
        // Cancel any existing animation
        _messageAnimationCts?.Cancel();
        _messageAnimationCts = new CancellationTokenSource();

        // Reset position and center vertically
        Canvas.SetLeft(MessageTextBlock, 0);
        CenterMessageVertically();

        // Start new animation if needed
        _isMessageAnimating = false;
        _ = StartMessageMarqueeAnimationAsync(_messageAnimationCts.Token);
    }

    private void CenterMessageVertically()
    {
        var canvasHeight = MessageCanvas.Bounds.Height;
        var textHeight = MessageTextBlock.Bounds.Height;

        if (canvasHeight > 0 && textHeight > 0)
        {
            var top = (canvasHeight - textHeight) / 2;
            Canvas.SetTop(MessageTextBlock, top);
        }
    }

    private async Task StartMessageMarqueeAnimationAsync(CancellationToken token)
    {
        if (_isMessageAnimating) return;

        try
        {
            _isMessageAnimating = true;

            // Wait for layout to settle
            await Task.Delay(300, token);

            // Ensure vertical centering
            await Dispatcher.UIThread.InvokeAsync(CenterMessageVertically);

            while (!token.IsCancellationRequested)
            {
                double canvasWidth = 0;
                double textWidth = 0;

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    canvasWidth = MessageCanvas.Bounds.Width;
                    textWidth = MessageTextBlock.Bounds.Width;
                });

                // Only scroll if text is wider than container
                if (textWidth > canvasWidth && canvasWidth > 0)
                {
                    var scrollDistance = textWidth - canvasWidth + 20; // Extra padding
                    var animationDuration = TimeSpan.FromMilliseconds(scrollDistance * 40); // Speed: 40ms per pixel

                    // Wait at start
                    await Task.Delay(2000, token);

                    // Scroll left
                    await AnimateCanvasLeftAsync(MessageTextBlock, 0, -scrollDistance, animationDuration, token);

                    // Wait at end
                    await Task.Delay(2000, token);

                    // Reset to start
                    await Dispatcher.UIThread.InvokeAsync(() => Canvas.SetLeft(MessageTextBlock, 0));

                    // Wait before repeating
                    await Task.Delay(1000, token);
                }
                else
                {
                    // Center the text if it fits
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        if (canvasWidth > 0 && textWidth > 0 && textWidth <= canvasWidth)
                        {
                            Canvas.SetLeft(MessageTextBlock, (canvasWidth - textWidth) / 2);
                        }
                    });

                    // Check again after a delay (in case text changes)
                    await Task.Delay(2000, token);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Animation was canceled, that's fine
        }
        finally
        {
            _isMessageAnimating = false;
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _marqueeAnimationCts?.Cancel();
        _messageAnimationCts?.Cancel();
        _restartDebounce?.Cancel();
        _messageRestartDebounce?.Cancel();
        Loaded -= OnLoaded;
        DataContextChanged -= OnDataContextChanged;
        TrackInfoPanel.PropertyChanged -= OnTrackInfoPanelPropertyChanged;
        TrackInfoCanvas.PropertyChanged -= OnTrackInfoCanvasPropertyChanged;
        MessageTextBlock.PropertyChanged -= OnMessageTextBlockPropertyChanged;
        MessageCanvas.PropertyChanged -= OnMessageCanvasPropertyChanged;

        // Unsubscribe from view model events
        SubscribeToViewModel(null);
    }
}
