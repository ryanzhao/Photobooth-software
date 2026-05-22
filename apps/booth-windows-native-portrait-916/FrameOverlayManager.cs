using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Photobooth.BoothNative;

public sealed class FrameOverlayManager
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly TemplateManager _templateManager;
    private readonly string _catalogFile;

    public FrameOverlayManager(TemplateManager templateManager)
    {
        _templateManager = templateManager;
        _catalogFile = Path.Combine(_templateManager.FramesRoot, "catalog.json");
    }

    public async Task InitializeAsync()
    {
        await _templateManager.InitializeAsync();
        if (!File.Exists(_catalogFile))
        {
            var frames = BuildDefaultFrames();
            await using var stream = File.Create(_catalogFile);
            await JsonSerializer.SerializeAsync(stream, frames, JsonOptions);
        }

        await EnsureStarterFrameAssetsAsync();
    }

    public async Task<List<NativeFrameRecord>> LoadFramesAsync()
    {
        await InitializeAsync();
        await using var stream = File.OpenRead(_catalogFile);
        var frames = await JsonSerializer.DeserializeAsync<List<NativeFrameRecord>>(stream, JsonOptions) ?? new List<NativeFrameRecord>();
        return frames
            .Where(frame => !string.IsNullOrWhiteSpace(frame.Id))
            .Select(Normalize)
            .OrderBy(frame => frame.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private NativeFrameRecord Normalize(NativeFrameRecord frame)
    {
        frame.OverlayPath = ResolveFramePath(frame.OverlayPath);
        frame.PreviewPath = string.IsNullOrWhiteSpace(frame.PreviewPath) ? frame.OverlayPath : ResolveFramePath(frame.PreviewPath);
        frame.ThumbnailPath = string.IsNullOrWhiteSpace(frame.ThumbnailPath) ? frame.OverlayPath : ResolveFramePath(frame.ThumbnailPath);
        return frame;
    }

    private string ResolveFramePath(string path)
    {
        if (Path.IsPathRooted(path))
        {
            return path;
        }

        return Path.GetFullPath(Path.Combine(_templateManager.FramesRoot, path));
    }

    private List<NativeFrameRecord> BuildDefaultFrames()
    {
        return
        [
            new NativeFrameRecord
            {
                Id = "frame-soft-gold",
                Name = "Soft Gold",
                Description = "Warm soft gold frame for 4x6 and strip prints.",
                OverlayPath = "frame-soft-gold.png",
                PreviewPath = "frame-soft-gold.png",
                ThumbnailPath = "frame-soft-gold.png",
                PreviewOpacity = 0.55d,
                ApplyMode = NativeOverlayApplyMode.GuideAndFinal
            },
            new NativeFrameRecord
            {
                Id = "frame-white-classic",
                Name = "White Classic",
                Description = "Clean white frame with subtle inner line.",
                OverlayPath = "frame-white-classic.png",
                PreviewPath = "frame-white-classic.png",
                ThumbnailPath = "frame-white-classic.png",
                PreviewOpacity = 0.62d,
                ApplyMode = NativeOverlayApplyMode.GuideAndFinal
            },
            new NativeFrameRecord
            {
                Id = "frame-peach-strip",
                Name = "Peach Strip",
                Description = "Light peach strip overlay for portrait strips.",
                OverlayPath = "frame-peach-strip.png",
                PreviewPath = "frame-peach-strip.png",
                ThumbnailPath = "frame-peach-strip.png",
                PreviewOpacity = 0.58d,
                ApplyMode = NativeOverlayApplyMode.GuideAndFinal
            }
        ];
    }

    private async Task EnsureStarterFrameAssetsAsync()
    {
        await Task.Run(() =>
        {
            CreateFrameIfMissing("frame-soft-gold.png", Colors.Transparent, Color.FromArgb(220, 216, 187, 135), Color.FromArgb(255, 242, 225, 190));
            CreateFrameIfMissing("frame-white-classic.png", Colors.Transparent, Color.FromArgb(230, 255, 255, 255), Color.FromArgb(255, 224, 215, 206));
            CreateFrameIfMissing("frame-peach-strip.png", Colors.Transparent, Color.FromArgb(225, 248, 219, 204), Color.FromArgb(255, 230, 168, 142));
        });
    }

    private void CreateFrameIfMissing(string fileName, Color background, Color fillColor, Color accentColor)
    {
        var path = Path.Combine(_templateManager.FramesRoot, fileName);
        if (File.Exists(path))
        {
            return;
        }

        const int width = 1200;
        const int height = 1800;
        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
        {
            dc.DrawRectangle(new SolidColorBrush(background), null, new Rect(0, 0, width, height));
            var outerPen = new Pen(new SolidColorBrush(fillColor), 78);
            var innerPen = new Pen(new SolidColorBrush(accentColor), 10);
            dc.DrawRoundedRectangle(null, outerPen, new Rect(38, 38, width - 76, height - 76), 34, 34);
            dc.DrawRoundedRectangle(null, innerPen, new Rect(110, 110, width - 220, height - 220), 22, 22);
        }

        SaveVisual(path, visual, width, height);
    }

    private void SaveVisual(string path, DrawingVisual visual, int width, int height)
    {
        var bitmap = new RenderTargetBitmap(width, height, 300, 300, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = File.Create(path);
        encoder.Save(stream);
    }
}
