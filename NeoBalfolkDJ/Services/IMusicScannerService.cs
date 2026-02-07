using System.Collections.Generic;
using NeoBalfolkDJ.Models;

namespace NeoBalfolkDJ.Services;

/// <summary>
/// Service for scanning directories for music files.
/// </summary>
public interface IMusicScannerService
{
    /// <summary>
    /// Scans a directory recursively for .mp3 files and parses track information from filenames.
    /// Expected filename pattern: "{dance} - {artist} - {title}.mp3"
    /// </summary>
    /// <param name="directoryPath">The root directory to scan</param>
    /// <returns>A list of Track objects parsed from the found files</returns>
    List<Track> ScanDirectory(string directoryPath);
}

