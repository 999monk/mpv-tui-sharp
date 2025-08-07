using MpvTuiSharp.Models;

namespace MpvTuiSharp.Services;

public class Library
{
    public readonly List<Album> _albums = new();
    private readonly string[] _supportedExtensions = { ".mp3", ".flac", ".m4a", ".ogg", ".wav", ".wma" };
    
    public IReadOnlyList<Album> Albums => _albums.AsReadOnly();
    public int AlbumCount => _albums.Count;
    public int TotalTracks => _albums.Sum(a => a.TrackCount);

    public async Task IndexDirectoryAsync(string path)
    {
        _albums.Clear();
        await Task.Run(() => IndexAlbumsRecursive(path));
        
        _albums.Sort((a, b) => 
        {
            var artistCompare = string.Compare(a.Artist, b.Artist, StringComparison.OrdinalIgnoreCase);
            return artistCompare != 0 ? artistCompare : string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
        });
    }

    private void IndexAlbumsRecursive(string path)
    {
        try
        {
            var musicFiles = Directory.GetFiles(path)
                .Where(f => _supportedExtensions.Contains(Path.GetExtension(f).ToLower()))
                .ToArray();
            if (musicFiles.Length > 0)
            {
                var album = CreateAlbumFromDirectory(path, musicFiles);
                if (album.Tracks.Count > 0)
                {
                    _albums.Add(album);
                }
            }
            // resolver anidados
            var directories = Directory.GetDirectories(path);
            foreach (var dir in directories)
            {
                IndexAlbumsRecursive(dir);
            }
        }
        catch(UnauthorizedAccessException)
        {
            // Ignorar directorios sin acceso
        }
        catch(Exception ex)
        {
            Console.WriteLine($"Error indexing {path}: {ex.Message}");
        }
    }

    private Album CreateAlbumFromDirectory(string directoryPath, string[] musicFiles)
    {
        var directoryName = Path.GetFileName(directoryPath);
        var album = new Album
        {
            DirectoryPath = directoryPath,
            Name = directoryName,
            Artist = "Unknown Artist" 
        };
        
        var tracks = new List<Track>();
        foreach (var file in musicFiles.OrderBy(f => f))
        {
            var track = ExtractMetadata(file, directoryName);
            if (track != null)
            {
                tracks.Add(track);
            }
        }

        if (tracks.Count > 0)
        {
            var artistGroups = tracks.GroupBy(t => t.Artist).OrderByDescending(g => g.Count());
            album.Artist = artistGroups.First().Key;
            
            if (directoryName.Contains(" - "))
            {
                var parts = directoryName.Split(" - ", 2);
                album.Artist = parts[0].Trim();
                album.Name = parts[1].Trim();
            }
        }
        album.Tracks = tracks;
        return album;
    }

    private Track? ExtractMetadata(string filePath, string albumName)
    {
        try
        {
            var fileName = Path.GetFileNameWithoutExtension(filePath);
            var trackNumber = ExtractTrackNumber(fileName);

            return new Track
            {
                FilePath = filePath,
                Title = CleanTrackTitle(fileName),
                Artist = "Unknown Artist",
                Album = albumName,
                TrackNumber = trackNumber
            };

        }
        catch
        {
            return null;
        }
    }

    private int ExtractTrackNumber(string fileName)
    {
        var parts = fileName.Split(new[] { ' ', '-', '.' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length > 0 && int.TryParse(parts[0], out int trackNum))
        {
            return trackNum;
        }
        return 0;
    }

    private string CleanTrackTitle(string fileName)
    {
        var cleanTitle = fileName;
        var parts = fileName.Split(new[] { " - ", ". " }, 2, StringSplitOptions.RemoveEmptyEntries);
            
        if (parts.Length > 1 && int.TryParse(parts[0], out _))
        {
            cleanTitle = parts[1];
        }
            
        return cleanTitle;
    }

    public List<Album> SearchAlbums(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return _albums;
        
        return _albums.Where(a => 
            a.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
            a.Artist.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
            a.Tracks.Any(t=> t.Title.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
        ).ToList();
    }

    public List<Album> GetAlbumsByArtist(string artist)
    {
        return _albums.Where(a => a.Artist.Contains(artist, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    public List<string> GetArtist()
    {
        return _albums.Select(a => a.Artist).Distinct().OrderBy(a => a).ToList();
    }

    public List<Track> GetAllTracks()
    {
        return _albums.SelectMany(a => a.Tracks).ToList();
    }
    
}