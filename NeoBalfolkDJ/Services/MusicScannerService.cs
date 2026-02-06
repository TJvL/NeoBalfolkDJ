using System;
using System.Collections.Generic;
using System.IO;
using NeoBalfolkDJ.Models;

namespace NeoBalfolkDJ.Services;

public static class MusicScannerService
{
    /// <summary>
    /// Scans a directory recursively for .mp3 files and parses track information from filenames.
    /// Expected filename pattern: "{dance} - {artist} - {title}.mp3"
    /// </summary>
    /// <param name="directoryPath">The root directory to scan</param>
    /// <returns>A list of Track objects parsed from the found files</returns>
    public static List<Track> ScanDirectory(string directoryPath)
    {
        var tracks = new List<Track>();

        if (string.IsNullOrWhiteSpace(directoryPath) || !Directory.Exists(directoryPath))
        {
            LoggingService.Warning($"Music directory does not exist or is empty: {directoryPath}");
            return tracks;
        }

        LoggingService.Info($"Scanning music directory: {directoryPath}");

        try
        {
            var mp3Files = Directory.EnumerateFiles(directoryPath, "*.mp3", SearchOption.AllDirectories);
            var skippedCount = 0;

            foreach (var filePath in mp3Files)
            {
                var track = ParseTrackFromFilePath(filePath);
                if (track != null)
                {
                    tracks.Add(track);
                }
                else
                {
                    skippedCount++;
                }
            }

            LoggingService.Info($"Scan complete: {tracks.Count} tracks found, {skippedCount} files skipped");
        }
        catch (Exception ex)
        {
            LoggingService.Error("Error scanning music directory", ex);
        }

        return tracks;
    }

    /// <summary>
    /// Parses a Track object from a file path.
    /// Expected filename pattern: "{dance} - {artist} - {title}.mp3"
    /// </summary>
    /// <param name="filePath">The full path to the MP3 file</param>
    /// <returns>A Track object if parsing succeeds, null otherwise</returns>
    private static Track? ParseTrackFromFilePath(string filePath)
    {
        try
        {
            var fileName = Path.GetFileNameWithoutExtension(filePath);
            
            // Split by " - " (space-dash-space)
            var parts = fileName.Split(" - ");
            
            // We need exactly 3 parts: dance, artist, title
            if (parts.Length != 3)
            {
                return null;
            }

            var dance = parts[0].Trim();
            var artist = parts[1].Trim();
            var title = parts[2].Trim();

            // Validate that none of the parts are empty
            if (string.IsNullOrWhiteSpace(dance) || 
                string.IsNullOrWhiteSpace(artist) || 
                string.IsNullOrWhiteSpace(title))
            {
                return null;
            }

            // Read track duration using TagLib
            var duration = GetTrackDuration(filePath);

            return new Track
            {
                Dance = dance,
                Artist = artist,
                Title = title,
                FilePath = filePath,
                Length = duration
            };
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// Reads the duration of an audio file using TagLib
    /// </summary>
    /// <param name="filePath">The path to the audio file</param>
    /// <returns>The duration of the track, or TimeSpan.Zero if it cannot be read</returns>
    private static TimeSpan GetTrackDuration(string filePath)
    {
        try
        {
            using var file = TagLib.File.Create(filePath);
            return file.Properties.Duration;
        }
        catch (Exception ex)
        {
            LoggingService.Warning($"Failed to read duration for {Path.GetFileName(filePath)}: {ex.Message}");
            return TimeSpan.Zero;
        }
    }
}
