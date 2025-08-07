using System;
using System.IO;

namespace MpvTuiSharp.Models;

public class Track
{
    public string FilePath { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Artist { get; set; } = string.Empty;
    public string Album { get; set; } = string.Empty;
    public int TrackNumber { get; set; }
    public TimeSpan Duration { get; set; }

    public string DisplayName => !string.IsNullOrEmpty(Title)
        ? $"{TrackNumber:D2} - {Title}"
        : Path.GetFileNameWithoutExtension(FilePath);
}