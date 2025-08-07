using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using MpvTuiSharp.Models;

namespace MpvTuiSharp.Services;

public class MpvController
{
    private Process? _mpvProcess;
    private NamedPipeClientStream? _pipeClient;
    private string _pipeName = "climusic-mpv";
    private bool _isInitialized = false;
    private string _musicPath = "";
    
    public bool IsPlaying { get; private set; }
    public bool IsPlayingAlbum { get; private set; }
    public bool IsShuffleMode { get; private set; }
    public string? CurrentTrackName { get; private set; }
    public Album? CurrentAlbum { get; private set; }
    public int CurrentTrackIndex { get; private set; }

    public async Task InitializeAsync(string musicPath)
    {
        if (_isInitialized) return;
        _musicPath = musicPath;
        try
        {
            _mpvProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "mpv",
                    Arguments = $"--no-video --idle --input-ipc-server={_pipeName}",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };
            _mpvProcess.OutputDataReceived += (sender, args) => { };
            _mpvProcess.ErrorDataReceived += (sender, args) => { };
            
            _mpvProcess.Start();
            
            _mpvProcess.BeginOutputReadLine();
            _mpvProcess.BeginErrorReadLine();
            
            await Task.Delay(1000);
            _pipeClient = new NamedPipeClientStream(".", _pipeName, PipeDirection.InOut);
            await _pipeClient.ConnectAsync(5000);
            _isInitialized = true;
            Console.WriteLine("mpv on.");
        }
        catch(Exception ex)
        {
            Console.WriteLine($"Error al iniciar mpv: {ex.Message}");
            Console.WriteLine("Recuerda que mpv tiene que estar instalado y en el PATH");
        }
    }

    public async Task PlayAsync(Track track, Album album, int trackIndex)
    {
        if (!_isInitialized) return;

        try
        {
            await SendCommandAsync(JsonSerializer.Serialize(new { command = new[] { "set", "shuffle", "no" } }));
            await SendCommandAsync(JsonSerializer.Serialize(new { command = new[] { "playlist-clear" } }));
            await SendCommandAsync(JsonSerializer.Serialize(new { command = new[] { "loadfile", track.FilePath } }));
                
            IsPlaying = true;
            IsPlayingAlbum = false;
            IsShuffleMode = false;
            CurrentTrackName = track.DisplayName;
            CurrentAlbum = album;
            CurrentTrackIndex = trackIndex;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error playing file: {ex.Message}");
        }
    }
    
    public async Task PlayAlbumAsync(Album album)
    {
        if (!_isInitialized || album.Tracks.Count == 0) return;

        try
        {
            await SendCommandAsync(JsonSerializer.Serialize(new { command = new[] { "set", "shuffle", "no" } }));
            await SendCommandAsync(JsonSerializer.Serialize(new { command = new[] { "playlist-clear" } }));
            await SendCommandAsync(JsonSerializer.Serialize(new { command = new[] { "loadfile", album.DirectoryPath } }));
                
            IsPlaying = true;
            IsPlayingAlbum = true;
            IsShuffleMode = false;
            CurrentAlbum = album;
            CurrentTrackIndex = 0;
            CurrentTrackName = album.Tracks[0].DisplayName;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error playing file: {ex.Message}");
        }
    }
    
    public void PlayShuffleAll()
    {
        if (!_isInitialized) return;

        try
        {
            IsPlaying = true;
            IsShuffleMode = true;
            IsPlayingAlbum = false;
            CurrentAlbum = null;
            CurrentTrackName = "Shuffle Mode";

            Task.Run(async () =>
            {
                await SendCommandAsync(JsonSerializer.Serialize(new { command = new[] { "set", "shuffle", "yes" } }));
                await SendCommandAsync(JsonSerializer.Serialize(new { command = new[] { "loadfile", _musicPath } }));
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error starting shuffle mode: {ex.Message}");
        }
    }

    public async Task NextTrackAsync()
    {
        if (!_isInitialized) return;
        await SendCommandAsync(JsonSerializer.Serialize(new { command = new[] { "playlist-next" } }));
    }

    public async Task PauseAsync()
    {
        if (!_isInitialized) return;

        var command = new { command = new[] { "cycle", "pause" } };
        await SendCommandAsync(JsonSerializer.Serialize(command));
        IsPlaying = !IsPlaying;
    }

    public async Task IncreaseVolumeAsync()
    {
        if (!_isInitialized) return;
        await SendCommandAsync(JsonSerializer.Serialize(new { command = new[] { "add", "volume", "5" } }));
    }

    public async Task DecreaseVolumeAsync()
    {
        if (!_isInitialized) return;
        await SendCommandAsync(JsonSerializer.Serialize(new { command = new[] { "add", "volume", "-5" } }));
    }

    public async Task StopAsync()
    {
        if (!_isInitialized) return;
        try
        {
            var command = new { command = new[] { "stop" } };
            await SendCommandAsync(JsonSerializer.Serialize(command));
            IsPlaying = false;
            IsShuffleMode = false;
            CurrentTrackName = null;
            CurrentAlbum = null;
            CurrentTrackIndex = -1;
        }
        finally
        {
            _pipeClient?.Close();
            _mpvProcess?.Kill();
            _mpvProcess?.Dispose();
        }
    }

    private async Task SendCommandAsync(string command)
    {
        if (_pipeClient == null || !_pipeClient.IsConnected) return;

        try
        {
            var json = command + "\n";
            var bytes = Encoding.UTF8.GetBytes(json);
            await _pipeClient.WriteAsync(bytes, 0, bytes.Length);
            await _pipeClient.FlushAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending command to mpv: {ex.Message}");
        }
    }
}
