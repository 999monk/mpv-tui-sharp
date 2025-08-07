using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MpvTuiSharp.Models;

public class Album
{
    public string DirectoryPath { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Artist { get; set; } = string.Empty;
    public int Year { get; set; }
    public List<Track> Tracks { get; set; } = new();
    public TimeSpan TotalDuration => TimeSpan.FromSeconds(Tracks.Sum(t => t.Duration.TotalSeconds));
    public int TrackCount => Tracks.Count;
    
    public string DisplayName => 
        !string.IsNullOrEmpty(Artist) && Artist != "Unknown Artist" 
            ? $"{Artist} - {Name} [{TrackCount} tracks]"
            : $"{Name} [{TrackCount} tracks]";
    
    public string ShortDisplayName =>
        !string.IsNullOrEmpty(Artist) && Artist != "Unknown Artist"
            ? $"{Artist} - {Name}"
            : Name;
}