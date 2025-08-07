using MpvTuiSharp.Models;

namespace MpvTuiSharp.Services;

public class UIManager
{
    private readonly Library _library;
    private readonly MpvController _mpv;
    private int _selectedIndex = 0;
    private ViewMode _currentMode = ViewMode.Albums;
    
    private Album[] _albumView;
    private Track[] _trackView = new Track[0];
    private Album? _selectedAlbum = null;
    private string _searchTerm = string.Empty;
    
    public UIManager(Library library, MpvController mpv)
    {
        _library = library;
        _mpv = mpv;
        _albumView = Array.Empty<Album>();
    }

    public void Initialize()
    {
        _albumView = _library.Albums.ToArray();
    }
    
    public async Task RunAsync()
    {
        RefreshView();

        while (true)
        {
            if (_currentMode == ViewMode.Search)
            {
                await HandleSearchInputAsync();
            }
            else
            {
                var key = Console.ReadKey(true);
                switch (key.Key)
                {
                    case ConsoleKey.Q:
                        return;
                        
                    case ConsoleKey.UpArrow:
                        NavigateUp();
                        break;
                        
                    case ConsoleKey.DownArrow:
                        NavigateDown();
                        break;
                        
                    case ConsoleKey.Enter:
                        await HandleEnter();
                        break;
                        
                    case ConsoleKey.Escape:
                        GoBack();
                        break;
                        
                    case ConsoleKey.Spacebar:
                        await _mpv.PauseAsync();
                        RefreshView();
                        break;
                        
                    case ConsoleKey.R:
                        RefreshView();
                        break;
                            
                    case ConsoleKey.P:
                        if (_currentMode == ViewMode.Albums)
                        {
                            await PlayAlbumFromAlbumView();
                        }
                        else if (_currentMode == ViewMode.Tracks && _selectedAlbum != null)
                        {
                            await PlayAlbum();
                        }
                        break;
                    case ConsoleKey.S:
                        if (_currentMode == ViewMode.Albums)
                        {
                            PlayShuffleAll();
                        }
                        break;
                            
                    case ConsoleKey.N:
                        await PlayNextTrack();
                        break;

                    case ConsoleKey.X:
                        await _mpv.IncreaseVolumeAsync();
                        break;

                    case ConsoleKey.Z:
                        await _mpv.DecreaseVolumeAsync();
                        break;
                    
                    case ConsoleKey.B:
                        if (_currentMode == ViewMode.Albums)
                        {
                            _currentMode = ViewMode.Search;
                            _searchTerm = string.Empty;
                            RefreshView();
                        }
                        break;
                }
            }
        }
    }
    
    private async Task HandleSearchInputAsync()
    {
        var key = Console.ReadKey(true);

        switch (key.Key)
        {
            case ConsoleKey.Escape:
                _currentMode = ViewMode.Albums;
                _searchTerm = string.Empty;
                _albumView = _library.Albums.ToArray();
                RefreshView();
                break;
            case ConsoleKey.Enter:
                _currentMode = ViewMode.Albums;
                RefreshView();
                break;
            case ConsoleKey.Backspace:
                if (_searchTerm.Length > 0)
                {
                    _searchTerm = _searchTerm.Substring(0, _searchTerm.Length - 1);
                }
                break;
            default:
                if (!char.IsControl(key.KeyChar))
                {
                    _searchTerm += key.KeyChar;
                }
                break;
        }

        _albumView = _library.SearchAlbums(_searchTerm).ToArray();
        _selectedIndex = 0;
        RefreshView();
    }

    private void ShowHeader()
    {
        Console.SetCursorPosition(0, 0);
        var modeText = _currentMode switch
        {
            ViewMode.Albums => "Albums",
            ViewMode.Tracks => $"Tracks - {_selectedAlbum?.ShortDisplayName}",
            ViewMode.Search => "Search",
            _ => ""
        };
        Console.WriteLine($"mpv-tui folder browser - {modeText}");
        Console.WriteLine(new string('=', Console.WindowWidth - 1));
        Console.WriteLine("↑/↓ nav | Enter sel | Space pause | P play album | S shuffle | z/x vol | b search | Esc back | Q quit");
        Console.WriteLine(new string('=', Console.WindowWidth - 1));
    }

    private void ShowCurrentView()
    {
        Console.SetCursorPosition(0, 4);
            
        if (_currentMode == ViewMode.Albums || _currentMode == ViewMode.Search)
        {
            ShowAlbums();
        }
        else
        {
            ShowTracks();
        }
    }

    private void ShowAlbums()
    {
        var startIndex = Math.Max(0, _selectedIndex - 10);
        var endIndex = Math.Min(_albumView.Length, startIndex + 15);
        
        for (int i = startIndex; i < endIndex; i++)
        {
            var album = _albumView[i];
            var isSelected = i == _selectedIndex;
                
            Console.BackgroundColor = isSelected ? ConsoleColor.DarkBlue : ConsoleColor.Black;
            Console.ForegroundColor = isSelected ? ConsoleColor.White : ConsoleColor.Gray;
                
            var displayText = album.DisplayName;
            if (displayText.Length > Console.WindowWidth - 5)
                displayText = displayText.Substring(0, Console.WindowWidth - 8) + "...";
                
            Console.WriteLine($" {displayText}".PadRight(Console.WindowWidth - 1));
            Console.ResetColor();
        }
    }

    private void ShowTracks()
    {
        if (_trackView.Length == 0) return;

        var startIndex = Math.Max(0, _selectedIndex - 10);
        var endIndex = Math.Min(_trackView.Length, startIndex + 15);

        for (int i = startIndex; i < endIndex; i++)
        {
            var track = _trackView[i];
            var isSelected = i == _selectedIndex;
            var isCurrentlyPlaying = _mpv.CurrentAlbum == _selectedAlbum && i == _mpv.CurrentTrackIndex;
                
            Console.BackgroundColor = isSelected ? ConsoleColor.DarkBlue : ConsoleColor.Black;
                
            if (isCurrentlyPlaying && _mpv.IsPlaying)
                Console.ForegroundColor = ConsoleColor.Green;
            else if (isSelected)
                Console.ForegroundColor = ConsoleColor.White;
            else
                Console.ForegroundColor = ConsoleColor.Gray;
                
            var displayText = track.DisplayName;
            if (displayText.Length > Console.WindowWidth - 5)
                displayText = displayText.Substring(0, Console.WindowWidth - 8) + "...";
                
            var prefix = isCurrentlyPlaying ? "♪ " : "  ";
            Console.WriteLine($"{prefix}{displayText}".PadRight(Console.WindowWidth - 1));
            Console.ResetColor();
        }
    }

    private void ShowControls()
    {
        var bottomLine = Console.WindowHeight - 2;
        Console.SetCursorPosition(0, bottomLine);
        Console.WriteLine(new string('=', Console.WindowWidth - 1));
        
        if (_currentMode == ViewMode.Search)
        {
            ShowSearchPrompt();
        }
        else
        {
            ShowStatus();
        }
    }
    
    private void ShowSearchPrompt()
    {
        var searchLine = Console.WindowHeight - 1;
        Console.SetCursorPosition(0, searchLine);
        Console.Write($"Search: {_searchTerm}".PadRight(Console.WindowWidth - 1));
    }

    private void ShowStatus()
    {
        var statusLine = Console.WindowHeight - 1;
        Console.SetCursorPosition(0, statusLine);

        string statusText;
        var status = _mpv.IsPlaying ? "Playing" : "Paused";

        if (_mpv.IsShuffleMode)
        {
            statusText = $"{status}: Shuffle Mode";
        }
        else if (_mpv.CurrentAlbum != null)
        {
            if (_mpv.IsPlayingAlbum)
            {
                statusText = $"{status} album: {_mpv.CurrentAlbum.DisplayName}";
            }
            else
            {
                statusText = $"{status}: {_mpv.CurrentTrackName} ({_mpv.CurrentAlbum.ShortDisplayName})";
            }
        }
        else
        {
            statusText = "Stopped";
        }

        Console.Write(statusText.PadLeft(Console.WindowWidth - 1));
    }

    private void NavigateUp()
    {
        var maxIndex = _currentMode == ViewMode.Albums ? _albumView.Length - 1 : _trackView.Length - 1;
        if (_selectedIndex > 0)
        {
            _selectedIndex--;
            RefreshView();
        }
    }

    private void NavigateDown()
    {
        var maxIndex = _currentMode == ViewMode.Albums ? _albumView.Length - 1 : _trackView.Length - 1;
        if (_selectedIndex < maxIndex)
        {
            _selectedIndex++;
            RefreshView();
        }
    }

    private async Task HandleEnter()
    {
        if (_currentMode == ViewMode.Albums)
        {
            if (_selectedIndex >= 0 && _selectedIndex < _albumView.Length)
            {
                _selectedAlbum = _albumView[_selectedIndex];
                _trackView = _selectedAlbum.Tracks.ToArray();
                _currentMode = ViewMode.Tracks;
                _selectedIndex = 0;
                RefreshView();
            }
        }
        else if (_currentMode == ViewMode.Tracks)
        {
            await PlaySelectedTrack();
        }
    }

    private void GoBack()
    {
        if (_currentMode == ViewMode.Tracks)
        {
            _currentMode = ViewMode.Albums;
            _selectedAlbum = null;
            _trackView = new Track[0];
            _selectedIndex = 0;
            RefreshView();
        }
        else if (_currentMode == ViewMode.Search)
        {
            _currentMode = ViewMode.Albums;
            _searchTerm = string.Empty;
            _albumView = _library.Albums.ToArray();
            RefreshView();
        }
    }

    private async Task PlaySelectedTrack()
    {
        if (_selectedIndex >= 0 && _selectedIndex < _trackView.Length && _selectedAlbum != null)
        {
            var selectedTrack = _trackView[_selectedIndex];
            await _mpv.PlayAsync(selectedTrack, _selectedAlbum, _selectedIndex);
            RefreshView();
        }
    }

    private async Task PlayAlbum()
    {
        if (_selectedAlbum != null && _selectedAlbum.Tracks.Count > 0)
        {
            await _mpv.PlayAlbumAsync(_selectedAlbum);
            _selectedIndex = 0;
            RefreshView();
        }
    }

    private async Task PlayAlbumFromAlbumView()
    {
        if (_selectedIndex >= 0 && _selectedIndex < _albumView.Length)
        {
            var album = _albumView[_selectedIndex];
            if (album.Tracks.Count > 0)
            {
                await _mpv.PlayAlbumAsync(album);
                RefreshView();
            }
        }
    }
    
    private void PlayShuffleAll()
    {
        _mpv.PlayShuffleAll();
        RefreshView();
    }

    private async Task PlayNextTrack()
    {
        if (_mpv.IsShuffleMode)
        {
            await _mpv.NextTrackAsync();
        }
        else if (_mpv.CurrentAlbum != null && _mpv.CurrentTrackIndex >= 0 && _mpv.CurrentTrackIndex < _mpv.CurrentAlbum.Tracks.Count - 1)
        {
            var nextTrackIndex = _mpv.CurrentTrackIndex + 1;
            var nextTrack = _mpv.CurrentAlbum.Tracks[nextTrackIndex];
            await _mpv.PlayAsync(nextTrack, _mpv.CurrentAlbum, nextTrackIndex);
            _selectedIndex = nextTrackIndex;
            RefreshView();
        }
    }

    private void RefreshView()
    {
            Console.Clear();
            ShowHeader();
            ShowCurrentView();
            ShowControls();
    }
}

public enum ViewMode
{
    Albums,
    Tracks,
    Search
}
