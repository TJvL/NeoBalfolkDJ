using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeoBalfolkDJ.Models;

namespace NeoBalfolkDJ.ViewModels;

public partial class NotificationViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _message = string.Empty;

    [ObservableProperty]
    private bool _isVisible;

    [ObservableProperty]
    private double _opacity;

    [ObservableProperty]
    private IBrush _backgroundColor = Brushes.DodgerBlue;

    private const int FadeDurationMs = 300;
    private const int DisplayDurationMs = 10000;
    
    private CancellationTokenSource? _cancellationTokenSource;

    public async Task ShowNotification(string message, NotificationSeverity severity)
    {
        // Cancel any existing notification
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource = new CancellationTokenSource();
        var token = _cancellationTokenSource.Token;
        
        Message = message;
        BackgroundColor = severity switch
        {
            NotificationSeverity.Information => Brushes.DodgerBlue,
            NotificationSeverity.Warning => Brushes.Orange,
            NotificationSeverity.Error => Brushes.Crimson,
            _ => Brushes.DodgerBlue
        };

        // Fade in
        IsVisible = true;
        await FadeIn(token);
        
        if (token.IsCancellationRequested)
            return;

        // Wait for display duration
        try
        {
            await Task.Delay(DisplayDurationMs, token);
        }
        catch (TaskCanceledException)
        {
            return;
        }

        // Fade out
        await FadeOut(token);
        
        if (!token.IsCancellationRequested)
        {
            IsVisible = false;
        }
    }

    [RelayCommand]
    public async Task CloseNotification()
    {
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource = new CancellationTokenSource();
        var token = _cancellationTokenSource.Token;
        
        await FadeOut(token);
        
        if (!token.IsCancellationRequested)
        {
            IsVisible = false;
        }
    }

    private async Task FadeIn(CancellationToken token)
    {
        const int steps = 15;
        const int stepDelay = FadeDurationMs / steps;

        for (int i = 0; i <= steps; i++)
        {
            if (token.IsCancellationRequested)
                return;
                
            Opacity = (double)i / steps;
            await Task.Delay(stepDelay, CancellationToken.None);
        }
    }

    private async Task FadeOut(CancellationToken token)
    {
        const int steps = 15;
        const int stepDelay = FadeDurationMs / steps;

        for (int i = steps; i >= 0; i--)
        {
            if (token.IsCancellationRequested)
                return;
                
            Opacity = (double)i / steps;
            await Task.Delay(stepDelay, CancellationToken.None);
        }
    }
}
