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
public sealed record StopMarker : IQueueItem
{
    public TimeSpan Duration => TimeSpan.Zero;
}

/// <summary>
/// Represents a delay/pause in the playback queue.
/// When reached, playback pauses for the specified duration then continues.
/// </summary>
public sealed record DelayMarker : IQueueItem
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
public sealed record MessageMarker : IQueueItem
{
    /// <summary>
    /// The custom message to display (max 60 characters)
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Optional delay in seconds. If null, behaves like a stop marker (waits for manual resume).
    /// </summary>
    public int? DelaySeconds { get; }

    /// <summary>
    /// Optional delay duration. If null, behaves like a stop marker (waits for manual resume).
    /// </summary>
    public TimeSpan? DelayDuration { get; }

    /// <summary>
    /// Returns true if this marker has a timed delay, false if it's a stop.
    /// </summary>
    public bool HasDelay => DelayDuration.HasValue;

    public TimeSpan Duration => DelayDuration ?? TimeSpan.Zero;

    public MessageMarker(string message, int? delaySeconds)
    {
        // Limit message to 60 characters
        Message = message.Length > 60 ? message[..60] : message;
        DelaySeconds = delaySeconds;
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
public sealed record AutoQueuedTrack(Track Track) : IQueueItem
{
    // Expose track properties for display
    public string Dance => Track.Dance;
    public string Artist => Track.Artist;
    public string Title => Track.Title;
    public string LengthFormatted => Track.LengthFormatted;

    public TimeSpan Duration => Track.Duration;
}

/// <summary>
/// Represents a music track.
/// </summary>
public sealed record Track(
    string Dance,
    string Artist,
    string Title,
    TimeSpan Length,
    string FilePath) : IQueueItem
{
    /// <summary>
    /// Gets the length formatted as m:ss
    /// </summary>
    public string LengthFormatted => $"{(int)Length.TotalMinutes}:{Length.Seconds:D2}";

    // IQueueItem implementation
    public TimeSpan Duration => Length;
}
