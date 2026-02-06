using CommunityToolkit.Mvvm.ComponentModel;
using NeoBalfolkDJ.Models;

namespace NeoBalfolkDJ.ViewModels;

public partial class PresentationDisplayViewModel : ViewModelBase
{
    // Current track info (top half)
    [ObservableProperty]
    private string _currentDance = string.Empty;

    [ObservableProperty]
    private string _currentArtist = string.Empty;

    [ObservableProperty]
    private string _currentTitle = string.Empty;

    // Next track info (bottom half)
    [ObservableProperty]
    private string _nextDance = string.Empty;

    [ObservableProperty]
    private string _nextArtist = string.Empty;

    [ObservableProperty]
    private string _nextTitle = string.Empty;

    [ObservableProperty]
    private bool _hasNextItem;

    // Progress bar
    [ObservableProperty]
    private double _progress;

    [ObservableProperty]
    private double _duration;

    [ObservableProperty]
    private bool _hasCurrentItem;

    [ObservableProperty]
    private bool _isMessageMode;

    public void UpdateCurrentTrack(Track? track)
    {
        IsMessageMode = false;
        if (track == null)
        {
            CurrentDance = string.Empty;
            CurrentArtist = string.Empty;
            CurrentTitle = string.Empty;
            HasCurrentItem = false;
        }
        else
        {
            CurrentDance = track.Dance;
            CurrentArtist = track.Artist;
            CurrentTitle = track.Title;
            HasCurrentItem = true;
        }
    }

    public void ShowStopMarker()
    {
        IsMessageMode = false;
        CurrentDance = "Stop";
        CurrentArtist = " ";
        CurrentTitle = string.Empty;
        HasCurrentItem = true;
    }

    public void ShowDelayMarker(long durationMs)
    {
        IsMessageMode = false;
        CurrentDance = "Delay";
        CurrentArtist = " ";
        CurrentTitle = string.Empty;
        Duration = durationMs;
        Progress = 0;
        HasCurrentItem = true;
    }

    public void ShowMessageMarker(string message, long? durationMs)
    {
        IsMessageMode = true;
        CurrentDance = message;
        CurrentArtist = " ";
        CurrentTitle = string.Empty;
        Duration = durationMs ?? 0;
        Progress = 0;
        HasCurrentItem = true;
    }

    public void UpdateNextItem(IQueueItem? item)
    {
        switch (item)
        {
            case null:
                NextDance = string.Empty;
                NextArtist = string.Empty;
                NextTitle = string.Empty;
                HasNextItem = false;
                break;
            case AutoQueuedTrack autoQueued:
                NextDance = autoQueued.Dance;
                NextArtist = autoQueued.Artist;
                NextTitle = autoQueued.Title;
                HasNextItem = true;
                break;
            case Track track:
                NextDance = track.Dance;
                NextArtist = track.Artist;
                NextTitle = track.Title;
                HasNextItem = true;
                break;
            case StopMarker:
                NextDance = "Stop";
                NextArtist = " ";
                NextTitle = string.Empty;
                HasNextItem = true;
                break;
            case DelayMarker delay:
                NextDance = "Delay";
                NextArtist = delay.DurationFormatted;
                NextTitle = string.Empty;
                HasNextItem = true;
                break;
            case MessageMarker message:
                NextDance = "Message";
                NextArtist = message.DurationFormatted;
                NextTitle = string.Empty;
                HasNextItem = true;
                break;
        }
    }

    public void UpdateProgress(double progress, double duration)
    {
        Progress = progress;
        Duration = duration;
    }

    public void Clear()
    {
        UpdateCurrentTrack(null);
        UpdateNextItem(null);
        Progress = 0;
        Duration = 0;
    }
}
