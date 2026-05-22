using System.Text.Json.Serialization;

namespace Photobooth.BoothNative;

public enum NativeBeautyLevel
{
    Off = 0,
    Low = 1,
    Medium = 2,
    High = 3
}

public enum NativeOverlayApplyMode
{
    FinalOnly = 0,
    GuideAndFinal = 1
}

public enum NativeSourceMode
{
    Camera = 0,
    Upload = 1,
    Gallery = 2
}

public enum NativePhotoSourceOrigin
{
    Camera = 0,
    Upload = 1,
    Gallery = 2
}

public enum NativePreviewStickerKind
{
    None = 0,
    DogEars = 1,
    PartyHat = 2,
    Hearts = 3
}

public enum NativePreviewMaskMode
{
    None = 0,
    LeftHalf = 1,
    RightHalf = 2,
    CenterSpotlight = 3
}

public sealed class NativeSessionRecord
{
    public string Id { get; set; } = string.Empty;
    public string Status { get; set; } = "draft";
    public string CaptureMode { get; set; } = "multi";
    public string SourceMode { get; set; } = nameof(NativeSourceMode.Camera);
    public int ShotCount { get; set; }
    public int RequiredShotCount { get; set; }
    public int CurrentShotIndex { get; set; }
    public int CountdownSeconds { get; set; }
    public string FolderPath { get; set; } = string.Empty;
    public string SourceFolderPath { get; set; } = string.Empty;
    public string RawFolderPath { get; set; } = string.Empty;
    public string ProcessedFolderPath { get; set; } = string.Empty;
    public string FinalFolderPath { get; set; } = string.Empty;
    public string MetadataFilePath { get; set; } = string.Empty;
    public string TemplateId { get; set; } = string.Empty;
    public string TemplateName { get; set; } = string.Empty;
    public string SelectedFrameId { get; set; } = string.Empty;
    public string SelectedBeautyLevel { get; set; } = nameof(NativeBeautyLevel.Off);
    public string SelectedEffectPresetId { get; set; } = "clean-modern";
    public string SourceAssignmentMode { get; set; } = "auto";
    public string? FinalPngPath { get; set; }
    public string? FinalJpgPath { get; set; }
    public int PhotoCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? LastCaptureAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public sealed class NativePhotoRecord
{
    public string Id { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string SourceFilePath { get; set; } = string.Empty;
    public string RawFilePath { get; set; } = string.Empty;
    public string ProcessedFilePath { get; set; } = string.Empty;
    public int SlotIndex { get; set; }
    public bool IsRetake { get; set; }
    public bool IsManuallyAssigned { get; set; }
    public int SourceOrder { get; set; }
    public double EditScale { get; set; } = 1d;
    public double EditRotation { get; set; }
    public double EditOffsetX { get; set; }
    public double EditOffsetY { get; set; }
    public string SourceOrigin { get; set; } = nameof(NativePhotoSourceOrigin.Camera);
    public string AppliedBeautyLevel { get; set; } = nameof(NativeBeautyLevel.Off);
    public string AppliedEffectPresetId { get; set; } = "clean-modern";
    public DateTime CapturedAt { get; set; }
}

public sealed class NativeTemplateSlotRecord
{
    public string Id { get; set; } = string.Empty;
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public double Rotation { get; set; }
    public double BorderRadius { get; set; }
    public string FitMode { get; set; } = "cover";
    public string Label { get; set; } = string.Empty;
}

public sealed class NativeTemplateTextRecord
{
    public string Id { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double FontSize { get; set; }
    public string FontFamily { get; set; } = "Segoe UI Semibold";
    public string ColorHex { get; set; } = "#5b422e";
    public string Alignment { get; set; } = "left";
    public bool UseSessionTimestamp { get; set; }
}

public sealed class NativeDecorativeLayerRecord
{
    public string Id { get; set; } = string.Empty;
    public string AssetPath { get; set; } = string.Empty;
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public double Rotation { get; set; }
    public double Opacity { get; set; } = 1d;
    public string PlacementMode { get; set; } = "absolute";
    public bool ShowInPreview { get; set; } = true;
}

public sealed class NativeFrameRecord
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string OverlayPath { get; set; } = string.Empty;
    public string PreviewPath { get; set; } = string.Empty;
    public string ThumbnailPath { get; set; } = string.Empty;
    public double PreviewOpacity { get; set; } = 0.65d;
    public NativeOverlayApplyMode ApplyMode { get; set; } = NativeOverlayApplyMode.GuideAndFinal;
    public bool IsBuiltIn { get; set; } = true;
}

public sealed class NativeEffectPresetRecord
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool BlackAndWhite { get; set; }
    public bool VintageTone { get; set; }
    public bool OverlayOnly { get; set; }
    public double Brightness { get; set; }
    public double Contrast { get; set; }
    public double WarmTone { get; set; }
    public double GrainAmount { get; set; }
    public double SoftBeautyBlend { get; set; }
}

public sealed class NativeTemplateRecord
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string PaperSize { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Orientation { get; set; } = "portrait";
    public string StyleFamily { get; set; } = "clean-editorial";
    public int ExportWidth { get; set; } = 1200;
    public int ExportHeight { get; set; } = 1800;
    public int Dpi { get; set; } = 300;
    public string BackgroundColorHex { get; set; } = "#f8f2e8";
    public string? BackgroundImagePath { get; set; }
    public string? DefaultFrameId { get; set; }
    public string? FinalOverlayPath { get; set; }
    public List<NativeTemplateSlotRecord> Slots { get; set; } = new();
    public List<NativeTemplateTextRecord> TextBlocks { get; set; } = new();
    public List<NativeDecorativeLayerRecord> DecorativeLayers { get; set; } = new();
}

public sealed class NativeSessionMetadata
{
    public NativeSessionRecord Session { get; set; } = new();
    public List<NativePhotoRecord> Photos { get; set; } = new();
    public NativeFrameRecord? SelectedFrame { get; set; }
    public NativeTemplateRecord? SelectedTemplate { get; set; }
    public NativeEffectPresetRecord? SelectedEffectPreset { get; set; }
    public string ExportPngPath { get; set; } = string.Empty;
    public string ExportJpgPath { get; set; } = string.Empty;
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
    public string PreferredWindowOrientation { get; set; } = "Portrait";
    public string? ActiveSessionId { get; set; }
    public string? SelectedTemplateId { get; set; }
    public string? SelectedFrameId { get; set; }
    public string SelectedBeautyLevel { get; set; } = nameof(NativeBeautyLevel.Off);
    public string SelectedSourceMode { get; set; } = nameof(NativeSourceMode.Camera);
    public string SelectedEffectPresetId { get; set; } = "clean-modern";
    public List<NativeSessionRecord> RecentSessions { get; set; } = new();
    public List<NativePhotoRecord> GalleryPhotos { get; set; } = new();
    public List<NativeTemplateRecord> Templates { get; set; } = new();
    public List<NativeFrameRecord> Frames { get; set; } = new();
    public List<NativeEffectPresetRecord> EffectPresets { get; set; } = new();
}

public sealed class CaptureResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? FilePath { get; set; }
}

public sealed class NativeRenderResult
{
    public string PngPath { get; set; } = string.Empty;
    public string JpgPath { get; set; } = string.Empty;
}

public sealed class NativeBeautyProfile
{
    public NativeBeautyLevel Level { get; set; } = NativeBeautyLevel.Off;
    public double SkinSmoothingStrength { get; set; }
    public double FaceLiftStrength { get; set; }
    public double ContrastStrength { get; set; }
    public double WarmToneStrength { get; set; }
    public double BlemishSofteningStrength { get; set; }

    public static NativeBeautyProfile FromLevel(NativeBeautyLevel level) => level switch
    {
        NativeBeautyLevel.Low => new NativeBeautyProfile
        {
            Level = level,
            SkinSmoothingStrength = 0.16d,
            FaceLiftStrength = 0.06d,
            ContrastStrength = 0.05d,
            WarmToneStrength = 0.03d,
            BlemishSofteningStrength = 0.08d
        },
        NativeBeautyLevel.Medium => new NativeBeautyProfile
        {
            Level = level,
            SkinSmoothingStrength = 0.28d,
            FaceLiftStrength = 0.10d,
            ContrastStrength = 0.09d,
            WarmToneStrength = 0.06d,
            BlemishSofteningStrength = 0.15d
        },
        NativeBeautyLevel.High => new NativeBeautyProfile
        {
            Level = level,
            SkinSmoothingStrength = 0.38d,
            FaceLiftStrength = 0.14d,
            ContrastStrength = 0.12d,
            WarmToneStrength = 0.08d,
            BlemishSofteningStrength = 0.22d
        },
        _ => new NativeBeautyProfile
        {
            Level = NativeBeautyLevel.Off
        }
    };
}

public sealed class NativeTemplateCatalog
{
    [JsonPropertyName("templates")]
    public List<NativeTemplateRecord> Templates { get; set; } = new();
}

public sealed class NativeEffectPresetCatalog
{
    [JsonPropertyName("presets")]
    public List<NativeEffectPresetRecord> Presets { get; set; } = new();
}
