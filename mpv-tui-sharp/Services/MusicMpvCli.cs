using MpvTuiSharp.Models;

namespace MpvTuiSharp.Services;

public class MusicMpvCli
{
    private readonly Library _library;
    private readonly MpvController _mpv;
    private readonly UIManager _ui;
    private readonly ConfigurationService _configService;
    private string _musicPath = "";

    public MusicMpvCli()
    {
        _library = new Library();
        _mpv = new MpvController();
        _configService = new ConfigurationService();
        _ui = new UIManager(_library, _mpv);
    }

    public async Task RunAsync()
    {
        Console.WriteLine("music folder navagation tui by monk999");

        if (!await LoadOrSetupConfiguration()) return;

        await IndexLibraryAsync();
        _ui.Initialize();
        await _mpv.InitializeAsync(_musicPath);
        await _ui.RunAsync();
        await _mpv.StopAsync();
    }

    private async Task<bool> LoadOrSetupConfiguration()
    {
        var config = _configService.LoadConfig();
        if (config == null || string.IsNullOrWhiteSpace(config.MusicPath) || !Directory.Exists(config.MusicPath))
        {
            Console.WriteLine("Music path not configured or invalid.");
            return SetupMusicPath();
        }

        _musicPath = config.MusicPath;
        return true;
    }

    private bool SetupMusicPath()
    {
        Console.WriteLine("Please enter the full path to your music library:");
        var path = Console.ReadLine();

        while (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
        {
            Console.WriteLine("Invalid path. Please enter a valid directory path:");
            path = Console.ReadLine();
        }

        var newConfig = new Config { MusicPath = path };
        _configService.SaveConfig(newConfig);
        _musicPath = path;
        Console.WriteLine("Configuration saved.");
        return true;
    }

    private async Task IndexLibraryAsync()
    {
        Console.WriteLine("Indexing music library (album-oriented)...");

        try
        {
            await _library.IndexDirectoryAsync(_musicPath);
            Console.WriteLine($"✓ Indexed {_library.AlbumCount} albums with {_library.TotalTracks} tracks");

            if (_library.AlbumCount == 0)
            {
                Console.WriteLine("No albums found.");
                Console.WriteLine("Expected structure: Artist - Album");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error indexing library: {ex.Message}");
        }
    }
}
