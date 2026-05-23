using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Photobooth.BoothNative;

public sealed class CameraDetectionService : IDisposable
{
    private const string DigiCamBaseUrl = "http://127.0.0.1:5513";
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromSeconds(2) };
    private readonly SemaphoreSlim _refreshGate = new(1, 1);
    private BoothRuntimeStatus _currentStatus = new();
    private string _lastFingerprint = string.Empty;

    public event EventHandler<BoothRuntimeStatus>? RuntimeStatusChanged;

    public BoothRuntimeStatus CurrentStatus => _currentStatus;

    public async Task<BoothRuntimeStatus> StartAsync()
    {
        return await RefreshAsync();
    }

    public async Task<BoothRuntimeStatus> RefreshAsync()
    {
        await _refreshGate.WaitAsync();
        try
        {
            var next = await BuildRuntimeStatusAsync();
            var fingerprint = CreateFingerprint(next);
            _currentStatus = next;
            if (!string.Equals(fingerprint, _lastFingerprint, StringComparison.Ordinal))
            {
                _lastFingerprint = fingerprint;
                RuntimeStatusChanged?.Invoke(this, next);
            }

            return next;
        }
        finally
        {
            _refreshGate.Release();
        }
    }

    public void NotifyDeviceTopologyChanged()
    {
        _ = Task.Run(RefreshAsync);
    }

    public void Dispose()
    {
        _httpClient.Dispose();
        _refreshGate.Dispose();
    }

    private async Task<BoothRuntimeStatus> BuildRuntimeStatusAsync()
    {
        var usbDevices = await QueryWindowsUsbDevicesAsync();
        var bridgeInstalled = FindInstalledBridgePaths().Any(File.Exists);
        var bridgeDevices = await QueryBridgeDevicesAsync();
        if (bridgeDevices.BridgeReachable)
        {
            await EnsureLiveViewWindowAsync();
        }

        var liveViewReachable = bridgeDevices.BridgeReachable && await IsLiveViewReachableAsync();

        var mergedDevices = MergeDevices(usbDevices, bridgeDevices.Devices, bridgeInstalled, bridgeDevices.BridgeReachable, liveViewReachable);
        var runtime = new BoothRuntimeStatus
        {
            BridgeInstalled = bridgeInstalled,
            BridgeReachable = bridgeDevices.BridgeReachable,
            LiveViewReachable = liveViewReachable,
            LiveViewUrl = liveViewReachable ? $"{DigiCamBaseUrl}/liveview.jpg" : null,
            UsbCameraCount = usbDevices.Count,
            BridgeCameraCount = bridgeDevices.Devices.Count,
            Devices = mergedDevices,
            UpdatedAt = DateTime.Now
        };

        runtime.StatusCode = DetermineStatusCode(runtime);
        runtime.StatusMessage = runtime.StatusCode;
        return runtime;
    }

    private async Task<List<NativeDeviceRecord>> QueryWindowsUsbDevicesAsync()
    {
        var devices = await QueryWindowsUsbDevicesViaPowershellAsync();
        if (devices.Count > 0)
        {
            return devices;
        }

        return await QueryWindowsUsbDevicesViaPnPUtilAsync();
    }

    private async Task<List<NativeDeviceRecord>> QueryWindowsUsbDevicesViaPowershellAsync()
    {
        const string script = @"
$devices = Get-PnpDevice -PresentOnly -ErrorAction SilentlyContinue |
    Where-Object {
        $_.Class -in @('Camera','Image','WPD','USB') -or
        $_.FriendlyName -match 'Canon|Sony|Nikon|Fujifilm|FUJIFILM|ILCE|EOS|Alpha|ZV|RX|camera|Camera|MTP|PTP'
    } |
    Select-Object InstanceId, FriendlyName, Class, Manufacturer, Status
$devices | ConvertTo-Json -Compress
";

        var output = await RunPowershellAsync(script);
        if (string.IsNullOrWhiteSpace(output))
        {
            return new List<NativeDeviceRecord>();
        }

        var json = output.Trim();
        if (string.Equals(json, "null", StringComparison.OrdinalIgnoreCase))
        {
            return new List<NativeDeviceRecord>();
        }

        try
        {
            if (json.StartsWith("[", StringComparison.Ordinal))
            {
                var results = JsonSerializer.Deserialize<List<PnpDeviceDto>>(json, JsonOptions) ?? new List<PnpDeviceDto>();
                return results.Select(MapPnpDevice).Where(IsCameraLike).DistinctBy(device => device.Id).OrderBy(device => device.Name, StringComparer.OrdinalIgnoreCase).ThenBy(device => device.Id, StringComparer.OrdinalIgnoreCase).ToList();
            }

            var single = JsonSerializer.Deserialize<PnpDeviceDto>(json, JsonOptions);
            return single is null ? new List<NativeDeviceRecord>() : new List<NativeDeviceRecord> { MapPnpDevice(single) };
        }
        catch
        {
            return new List<NativeDeviceRecord>();
        }
    }

    private async Task<List<NativeDeviceRecord>> QueryWindowsUsbDevicesViaPnPUtilAsync()
    {
        var output = await RunProcessAsync("pnputil", "/enum-devices /connected");
        if (string.IsNullOrWhiteSpace(output))
        {
            return new List<NativeDeviceRecord>();
        }

        var devices = new List<PnpDeviceDto>();
        var blocks = Regex.Split(output.Trim(), @"\r?\n\r?\n");
        foreach (var block in blocks)
        {
            if (!block.Contains("Instance ID:", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var dto = new PnpDeviceDto
            {
                InstanceId = MatchValue(block, "Instance ID:"),
                FriendlyName = MatchValue(block, "Device Description:"),
                Class = MatchValue(block, "Class Name:"),
                Manufacturer = MatchValue(block, "Manufacturer Name:"),
                Status = MatchValue(block, "Status:")
            };

            if (!string.IsNullOrWhiteSpace(dto.InstanceId))
            {
                devices.Add(dto);
            }
        }

        return devices.Select(MapPnpDevice).Where(IsCameraLike).DistinctBy(device => device.Id).OrderBy(device => device.Name, StringComparer.OrdinalIgnoreCase).ThenBy(device => device.Id, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private async Task<(bool BridgeReachable, List<NativeDeviceRecord> Devices)> QueryBridgeDevicesAsync()
    {
        try
        {
            using var response = await _httpClient.GetAsync($"{DigiCamBaseUrl}/?slc=list&param1=cameras&param2=");
            if (!response.IsSuccessStatusCode)
            {
                return (false, new List<NativeDeviceRecord>());
            }

            var text = await response.Content.ReadAsStringAsync();
            var lines = text.Split(new[] { '\r', '\n', ',' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(name => new NativeDeviceRecord
                {
                    Id = $"bridge:{NormalizeId(name)}",
                    Kind = "camera",
                    Source = "bridge",
                    Name = name,
                    Transport = "USB Tethered",
                    ConnectionState = "connected",
                    RemoteTriggerSupported = true,
                    TransferSupported = true,
                    LiveViewSupported = true,
                    Diagnostics = "bridge_online"
                })
                .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            return (true, lines);
        }
        catch
        {
            return (false, new List<NativeDeviceRecord>());
        }
    }

    private List<NativeDeviceRecord> MergeDevices(List<NativeDeviceRecord> usbDevices, List<NativeDeviceRecord> bridgeDevices, bool bridgeInstalled, bool bridgeReachable, bool liveViewReachable)
    {
        var remainingUsb = new List<NativeDeviceRecord>(usbDevices);
        var merged = new List<NativeDeviceRecord>();

        foreach (var bridgeDevice in bridgeDevices)
        {
            var matchedUsb = remainingUsb.FirstOrDefault(usb => NamesLikelyMatch(usb.Name, bridgeDevice.Name));
            if (matchedUsb is not null)
            {
                remainingUsb.Remove(matchedUsb);
                merged.Add(new NativeDeviceRecord
                {
                    Id = matchedUsb.Id,
                    Kind = "camera",
                    Source = "usb+bridge",
                    Name = matchedUsb.Name,
                    Transport = "USB Tethered",
                    ConnectionState = "connected",
                    RemoteTriggerSupported = true,
                    TransferSupported = true,
                    LiveViewSupported = liveViewReachable,
                    Diagnostics = liveViewReachable ? "bridge_online" : "bridge_online_liveview_unavailable"
                });
            }
            else
            {
                bridgeDevice.LiveViewSupported = liveViewReachable;
                bridgeDevice.Diagnostics = liveViewReachable ? "bridge_online" : "bridge_online_liveview_unavailable";
                merged.Add(bridgeDevice);
            }
        }

        foreach (var usb in remainingUsb)
        {
            usb.Diagnostics = bridgeReachable ? "usb_detected_waiting_bridge_pairing" : bridgeInstalled ? "usb_detected_bridge_not_running" : "usb_detected_bridge_missing";
            usb.ConnectionState = "connected";
            merged.Add(usb);
        }

        if (merged.Count == 0)
        {
            merged.Add(new NativeDeviceRecord
            {
                Id = "placeholder:no-camera",
                Kind = "placeholder",
                Source = "system",
                Name = "Bridge / Camera Not Ready",
                Transport = "Tethered",
                ConnectionState = "disconnected",
                RemoteTriggerSupported = false,
                TransferSupported = false,
                LiveViewSupported = false,
                Diagnostics = bridgeReachable ? "bridge_online_no_devices" : bridgeInstalled ? "bridge_not_running" : "bridge_missing"
            });
        }

        merged.Add(new NativeDeviceRecord
        {
            Id = "fallback:webcam",
            Kind = "webcam-fallback",
            Source = "fallback",
            Name = "Webcam Fallback",
            Transport = "Webcam",
            ConnectionState = "ready",
            RemoteTriggerSupported = false,
            TransferSupported = false,
            LiveViewSupported = true,
            Diagnostics = "webcam_fallback"
        });

        return merged.OrderBy(DeviceSortRank).ThenBy(device => device.Name, StringComparer.OrdinalIgnoreCase).ThenBy(device => device.Id, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private string DetermineStatusCode(BoothRuntimeStatus runtime)
    {
        if (runtime.BridgeReachable && runtime.BridgeCameraCount > 0)
        {
            return runtime.LiveViewReachable ? "bridge_online_devices" : "bridge_online_liveview_unavailable";
        }

        if (runtime.BridgeReachable)
        {
            return runtime.UsbCameraCount > 0 ? "usb_detected_waiting_bridge_pairing" : "bridge_online_no_devices";
        }

        if (runtime.UsbCameraCount > 0)
        {
            return runtime.BridgeInstalled ? "usb_detected_bridge_not_running" : "usb_detected_bridge_missing";
        }

        return runtime.BridgeInstalled ? "bridge_not_running" : "bridge_missing";
    }

    private async Task<bool> IsLiveViewReachableAsync()
    {
        try
        {
            using var response = await _httpClient.GetAsync($"{DigiCamBaseUrl}/liveview.jpg");
            return response.IsSuccessStatusCode && (response.Content.Headers.ContentLength ?? 0) > 0;
        }
        catch
        {
            return false;
        }
    }

    private async Task EnsureLiveViewWindowAsync()
    {
        try
        {
            using var response = await _httpClient.GetAsync($"{DigiCamBaseUrl}/?CMD=LiveViewWnd_Show");
            _ = response.IsSuccessStatusCode;
        }
        catch
        {
        }
    }

    private async Task<string> RunPowershellAsync(string script)
    {
        var sanitizedScript = script.Replace("\r", string.Empty).Replace("\n", "; ");
        return await RunProcessAsync("powershell", $"-NoProfile -ExecutionPolicy Bypass -Command \"{sanitizedScript.Replace("\"", "`\"")}\"");
    }

    private async Task<string> RunProcessAsync(string fileName, string arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = startInfo };
        process.Start();
        var stdout = await process.StandardOutput.ReadToEndAsync();
        await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        return process.ExitCode == 0 ? stdout : string.Empty;
    }

    private static NativeDeviceRecord MapPnpDevice(PnpDeviceDto dto)
    {
        var name = string.IsNullOrWhiteSpace(dto.FriendlyName) ? dto.InstanceId : dto.FriendlyName;
        return new NativeDeviceRecord
        {
            Id = string.IsNullOrWhiteSpace(dto.InstanceId) ? $"usb:{NormalizeId(name)}" : dto.InstanceId,
            Kind = "camera",
            Source = "usb",
            Name = name ?? "USB Camera",
            Transport = "USB Tethered",
            ConnectionState = string.Equals(dto.Status, "Started", StringComparison.OrdinalIgnoreCase) || string.Equals(dto.Status, "OK", StringComparison.OrdinalIgnoreCase) ? "connected" : "attention",
            RemoteTriggerSupported = false,
            TransferSupported = false,
            LiveViewSupported = false,
            Diagnostics = "usb_detected_waiting_bridge_pairing"
        };
    }

    private static bool IsCameraLike(NativeDeviceRecord device)
    {
        var text = $"{device.Name} {device.Id}";
        return text.Contains("canon", StringComparison.OrdinalIgnoreCase)
            || text.Contains("sony", StringComparison.OrdinalIgnoreCase)
            || text.Contains("nikon", StringComparison.OrdinalIgnoreCase)
            || text.Contains("fujifilm", StringComparison.OrdinalIgnoreCase)
            || text.Contains("camera", StringComparison.OrdinalIgnoreCase)
            || text.Contains("eos", StringComparison.OrdinalIgnoreCase)
            || text.Contains("ilce", StringComparison.OrdinalIgnoreCase)
            || text.Contains("mtp", StringComparison.OrdinalIgnoreCase)
            || text.Contains("ptp", StringComparison.OrdinalIgnoreCase)
            || string.Equals(device.Transport, "USB Tethered", StringComparison.OrdinalIgnoreCase) && (text.Contains("vid_04a9", StringComparison.OrdinalIgnoreCase) || text.Contains("wpd", StringComparison.OrdinalIgnoreCase));
    }

    private static string MatchValue(string block, string label)
    {
        var match = Regex.Match(block, $"^{Regex.Escape(label)}\\s*(.+)$", RegexOptions.Multiline | RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value.Trim() : string.Empty;
    }

    private static bool NamesLikelyMatch(string left, string right)
    {
        var a = NormalizeId(left);
        var b = NormalizeId(right);
        return !string.IsNullOrWhiteSpace(a) && !string.IsNullOrWhiteSpace(b) && (a.Contains(b, StringComparison.OrdinalIgnoreCase) || b.Contains(a, StringComparison.OrdinalIgnoreCase));
    }

    private static string NormalizeId(string? value)
    {
        return Regex.Replace(value ?? string.Empty, "[^a-zA-Z0-9]", string.Empty).ToLowerInvariant();
    }

    private static int DeviceSortRank(NativeDeviceRecord device) => device.Kind switch
    {
        "camera" when device.Source == "usb+bridge" => 0,
        "camera" when device.Source == "bridge" => 1,
        "camera" when device.Source == "usb" => 2,
        "placeholder" => 3,
        "webcam-fallback" => 4,
        _ => 5
    };

    private static string CreateFingerprint(BoothRuntimeStatus status)
    {
        var deviceFingerprint = string.Join("|", status.Devices.Select(device => string.Join("~", new[]
        {
            device.Id,
            device.Name,
            device.Source,
            device.ConnectionState,
            device.Diagnostics,
            device.LiveViewSupported ? "1" : "0"
        })));

        return string.Join("#", new[]
        {
            status.BridgeInstalled ? "1" : "0",
            status.BridgeReachable ? "1" : "0",
            status.LiveViewReachable ? "1" : "0",
            status.StatusCode,
            status.UsbCameraCount.ToString(),
            status.BridgeCameraCount.ToString(),
            deviceFingerprint
        });
    }

    private IEnumerable<string> FindInstalledBridgePaths()
    {
        yield return @"D:\rocket\photobooth\digitcamcontrol\CameraControlRemoteCmd.exe";
        yield return @"D:\rocket\photobooth\digitcamcontrol\CameraControl.exe";
        yield return @"C:\Program Files (x86)\digiCamControl\CameraControlRemoteCmd.exe";
        yield return @"C:\Program Files (x86)\digiCamControl\CameraControl.exe";
        yield return @"C:\Program Files\digiCamControl\CameraControlRemoteCmd.exe";
        yield return @"C:\Program Files\digiCamControl\CameraControl.exe";
    }

    private sealed class PnpDeviceDto
    {
        public string? InstanceId { get; set; }
        public string? FriendlyName { get; set; }
        public string? Class { get; set; }
        public string? Manufacturer { get; set; }
        public string? Status { get; set; }
    }
}










