namespace Photobooth.BoothNative;

public sealed class NativeSessionRecord
{
    public string Id { get; set; } = string.Empty;
    public string Status { get; set; } = "draft";
    public string CaptureMode { get; set; } = "multi";
    public int ShotCount { get; set; }
    public int CountdownSeconds { get; set; }
    public string FolderPath { get; set; } = string.Empty;
    public int PhotoCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastCaptureAt { get; set; }
}

public sealed class NativePhotoRecord
{
    public string Id { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public DateTime CapturedAt { get; set; }
}

public sealed class NativeTemplateRecord
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string PaperSize { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public sealed class NativeDeviceRecord
{
    public string Id { get; set; } = string.Empty;
    public string Kind { get; set; } = "camera";
    public string Source { get; set; } = "unknown";
    public string Name { get; set; } = string.Empty;
    public string Transport { get; set; } = string.Empty;
    public string ConnectionState { get; set; } = "disconnected";
    public bool RemoteTriggerSupported { get; set; }
    public bool TransferSupported { get; set; }
    public bool LiveViewSupported { get; set; }
    public string Diagnostics { get; set; } = string.Empty;
}

public sealed class BoothRuntimeStatus
{
    public bool BridgeInstalled { get; set; }
    public bool BridgeReachable { get; set; }
    public bool LiveViewReachable { get; set; }
    public int UsbCameraCount { get; set; }
    public int BridgeCameraCount { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
    public string? LiveViewUrl { get; set; }
    public string StatusCode { get; set; } = "bridge_missing";
    public string StatusMessage { get; set; } = string.Empty;
    public List<NativeDeviceRecord> Devices { get; set; } = new();
}

public sealed class BoothSnapshot
{
    public string PreferredLanguage { get; set; } = "zh-CN";
    public string? ActiveSessionId { get; set; }
    public string? SelectedTemplateId { get; set; }
    public List<NativeSessionRecord> RecentSessions { get; set; } = new();
    public List<NativePhotoRecord> GalleryPhotos { get; set; } = new();
    public List<NativeTemplateRecord> Templates { get; set; } = new();
}

public sealed class CaptureResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? FilePath { get; set; }
}
