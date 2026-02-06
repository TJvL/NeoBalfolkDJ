using System;

namespace NeoBalfolkDJ.Models;

/// <summary>
/// Base interface for items that can be placed in the playback queue.
/// </summary>
public interface IQueueItem
{
    /// <summary>
    /// Duration of this item (zero for markers)
    /// </summary>
    TimeSpan Duration { get; }
}

/// <summary>
/// Represents a stop request in the playback queue.
/// When reached, playback stops and waits for user action.
/// </summary>
public class StopMarker : IQueueItem
{
    public TimeSpan Duration => TimeSpan.Zero;
}

/// <summary>
/// Represents a delay/pause in the playback queue.
/// When reached, playback pauses for the specified duration then continues.
/// </summary>
public class DelayMarker : IQueueItem
{
    public TimeSpan Duration { get; }
    
    public DelayMarker(int delaySeconds)
    {
        Duration = TimeSpan.FromSeconds(delaySeconds);
    }
    
    public string DurationFormatted => $"{(int)Duration.TotalMinutes}:{Duration.Seconds:D2}";
}

/// <summary>
/// Represents a message request in the playback queue.
/// When reached, displays a custom message and either stops (manual resume) or delays (timed).
/// </summary>
public class MessageMarker : IQueueItem
{
    /// <summary>
    /// The custom message to display (max 60 characters)
    /// </summary>
    public string Message { get; }
    
    /// <summary>
    /// Optional delay duration. If null, behaves like a stop marker (waits for manual resume).
    /// </summary>
    public TimeSpan? DelayDuration { get; }
    
    /// <summary>
    /// Returns true if this marker has a timed delay, false if it's a stop.
    /// </summary>
    public bool HasDelay => DelayDuration.HasValue;
    
    public TimeSpan Duration => DelayDuration ?? TimeSpan.Zero;
    
    public MessageMarker(string message, int? delaySeconds = null)
    {
        // Limit message to 60 characters
        Message = message.Length > 60 ? message.Substring(0, 60) : message;
        DelayDuration = delaySeconds.HasValue ? TimeSpan.FromSeconds(delaySeconds.Value) : null;
    }
    
    public string DurationFormatted => DelayDuration.HasValue 
        ? $"{(int)DelayDuration.Value.TotalMinutes}:{DelayDuration.Value.Seconds:D2}" 
        : "stop";
}

/// <summary>
/// Represents an automatically queued track that will be removed when
/// any manual queue operation occurs (adding items, clearing, stopping).
/// </summary>
public class AutoQueuedTrack : IQueueItem
{
    public Track Track { get; }
    
    public AutoQueuedTrack(Track track)
    {
        Track = track;
    }
    
    // Expose track properties for display
    public string Dance => Track.Dance;
    public string Artist => Track.Artist;
    public string Title => Track.Title;
    public string LengthFormatted => Track.LengthFormatted;
    
    public TimeSpan Duration => Track.Duration;
}

public class Track : IQueueItem
{
    public string Dance { get; set; } = string.Empty;
    public string Artist { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public TimeSpan Length { get; set; }
    
    /// <summary>
    /// Gets the length formatted as m:ss
    /// </summary>
    public string LengthFormatted => $"{(int)Length.TotalMinutes}:{Length.Seconds:D2}";
    
    /// <summary>
    /// The full file path to the audio file
    /// </summary>
    public string FilePath { get; set; } = string.Empty;
    
    // IQueueItem implementation
    public TimeSpan Duration => Length;

    // Equality based on FilePath (unique identifier for a track)
    public override bool Equals(object? obj)
    {
        if (obj is Track other)
        {
            return FilePath == other.FilePath;
        }
        return false;
    }

    public override int GetHashCode()
    {
        return FilePath.GetHashCode();
    }
}
