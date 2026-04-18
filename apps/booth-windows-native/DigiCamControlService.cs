using System.IO;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;

namespace Photobooth.BoothNative;

public sealed class DigiCamControlService : IDisposable
{
    private const string BaseUrl = "http://127.0.0.1:5513";
    private readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromSeconds(4) };

    public async Task<bool> LaunchOrAttachAsync()
    {
        if (await IsBridgeReachableAsync())
        {
            await EnsureLiveViewWindowAsync();
            return true;
        }

        var running = Process.GetProcessesByName("CameraControl").FirstOrDefault();
        if (running is null)
        {
            var executable = FindBridgeExecutablePath();
            if (string.IsNullOrWhiteSpace(executable) || !File.Exists(executable))
            {
                return false;
            }

            Process.Start(new ProcessStartInfo(executable)
            {
                UseShellExecute = true,
                WorkingDirectory = Path.GetDirectoryName(executable) ?? AppContext.BaseDirectory
            });
        }

        var deadline = DateTime.UtcNow.AddSeconds(18);
        while (DateTime.UtcNow < deadline)
        {
            if (await IsBridgeReachableAsync())
            {
                await EnsureLiveViewWindowAsync();
                return true;
            }

            await Task.Delay(300);
        }

        return false;
    }

    public async Task<bool> InitializeLiveBridgeAsync(bool launchIfNeeded = true)
    {
        if (launchIfNeeded && !await IsBridgeReachableAsync())
        {
            await LaunchOrAttachAsync();
        }

        if (!await IsBridgeReachableAsync())
        {
            return false;
        }

        await WarmUpBridgeAsync();
        await EnsureLiveViewWindowAsync();
        await Task.Delay(250);
        return await IsBridgeReachableAsync();
    }

    public async Task<bool> IsBridgeReachableAsync()
    {
        try
        {
            using var response = await _httpClient.GetAsync($"{BaseUrl}/?slc=list&param1=cameras&param2=");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
    public async Task<bool> EnsureLiveViewWindowAsync()
    {
        return await SendCommandAsync("LiveViewWnd_Show");
    }

    public async Task<bool> WarmUpBridgeAsync()
    {
        try
        {
            using var response = await _httpClient.GetAsync($"{BaseUrl}/?slc=list&param1=cameras&param2=");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> AutoFocusAsync()
    {
        await EnsureLiveViewWindowAsync();
        return await SendCommandAsync("LiveView_Focus");
    }

    public async Task<IReadOnlyList<string>> ListParameterValuesAsync(string key)
    {
        var content = await SendSingleCommandAsync("list", key, string.Empty);
        return content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => !string.IsNullOrWhiteSpace(x) && x != "?")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task<string?> GetParameterValueAsync(string key)
    {
        var content = await SendSingleCommandAsync("get", key, string.Empty);
        var value = content.Trim();
        return string.IsNullOrWhiteSpace(value) || value == "?" ? null : value;
    }

    public async Task<bool> SetParameterAsync(string key, string value)
    {
        var content = await SendSingleCommandAsync("set", key, value);
        return !string.IsNullOrWhiteSpace(content) && !content.Contains("error", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<CaptureResult> CapturePhotoAsync(string targetFolder, string filePrefix)
    {
        Directory.CreateDirectory(targetFolder);
        var before = GetKnownFiles(targetFolder);
        var folderSet = await SetParameterAsync("session.folder", targetFolder);
        if (!folderSet)
        {
            return new CaptureResult { Success = false, Message = "Unable to point digiCamControl to the current session folder." };
        }

        await EnsureLiveViewWindowAsync();
        var captureTriggered = await TriggerCaptureAsync();
        if (!captureTriggered)
        {
            return new CaptureResult { Success = false, Message = "The camera did not accept the capture command." };
        }

        var deadline = DateTime.UtcNow.AddSeconds(20);
        while (DateTime.UtcNow < deadline)
        {
            var newest = GetNewestImportedFile(targetFolder, before);
            if (newest is not null)
            {
                var normalized = Path.Combine(targetFolder, $"{filePrefix}{newest.Extension.ToLowerInvariant()}");
                var finalPath = newest.FullName;
                if (!string.Equals(newest.FullName, normalized, StringComparison.OrdinalIgnoreCase))
                {
                    if (File.Exists(normalized))
                    {
                        normalized = Path.Combine(targetFolder, $"{filePrefix}_{DateTimeOffset.UtcNow:HHmmssfff}{newest.Extension.ToLowerInvariant()}");
                    }

                    File.Move(newest.FullName, normalized);
                    finalPath = normalized;
                }

                return new CaptureResult
                {
                    Success = true,
                    Message = "Capture completed and saved to the local session.",
                    FilePath = finalPath
                };
            }

            await Task.Delay(300);
        }

        return new CaptureResult { Success = false, Message = "Capture was triggered, but no transferred photo appeared in the session folder within 20 seconds." };
    }

    private async Task<bool> TriggerCaptureAsync()
    {
        if (await TrySendCaptureCommandAsync("LiveView_Capture"))
        {
            return true;
        }

        return await TrySendCaptureCommandAsync("Capture");
    }

    private async Task<bool> TrySendCaptureCommandAsync(string command)
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(1500));
            using var response = await _httpClient.GetAsync($"{BaseUrl}/?CMD={Uri.EscapeDataString(command)}", HttpCompletionOption.ResponseHeadersRead, cts.Token);
            return response.IsSuccessStatusCode;
        }
        catch (TaskCanceledException)
        {
            return true;
        }
        catch
        {
            return false;
        }
    }
    public void Dispose()
    {
        _httpClient.Dispose();
    }

    private async Task<bool> SendCommandAsync(string command)
    {
        try
        {
            using var response = await _httpClient.GetAsync($"{BaseUrl}/?CMD={Uri.EscapeDataString(command)}");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private async Task<string> SendSingleCommandAsync(string slc, string param1, string param2)
    {
        try
        {
            var url = $"{BaseUrl}/?slc={Uri.EscapeDataString(slc)}&param1={Uri.EscapeDataString(param1)}&param2={Uri.EscapeDataString(param2)}";
            using var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                return string.Empty;
            }

            return await response.Content.ReadAsStringAsync();
        }
        catch
        {
            return string.Empty;
        }
    }

    private static HashSet<string> GetKnownFiles(string targetFolder)
    {
        return Directory.Exists(targetFolder)
            ? Directory.EnumerateFiles(targetFolder)
                .Where(IsImageFile)
                .Select(Path.GetFullPath)
                .ToHashSet(StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }

    private static FileInfo? GetNewestImportedFile(string targetFolder, HashSet<string> knownFiles)
    {
        if (!Directory.Exists(targetFolder))
        {
            return null;
        }

        return new DirectoryInfo(targetFolder)
            .EnumerateFiles()
            .Where(x => IsImageFile(x.FullName) && !knownFiles.Contains(Path.GetFullPath(x.FullName)))
            .OrderByDescending(x => x.LastWriteTimeUtc)
            .FirstOrDefault();
    }

    private static string? FindBridgeExecutablePath()
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "CameraControl.exe"),
            @"D:\rocket\photobooth\digitcamcontrol\CameraControl.exe",
            @"C:\Program Files (x86)\digiCamControl\CameraControl.exe",
            @"C:\Program Files\digiCamControl\CameraControl.exe"
        };

        return candidates.FirstOrDefault(File.Exists);
    }
    private static bool IsImageFile(string path)
    {
        var ext = Path.GetExtension(path);
        return ext.Equals(".jpg", StringComparison.OrdinalIgnoreCase)
            || ext.Equals(".jpeg", StringComparison.OrdinalIgnoreCase)
            || ext.Equals(".png", StringComparison.OrdinalIgnoreCase);
    }
}





