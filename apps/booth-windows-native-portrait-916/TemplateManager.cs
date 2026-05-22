using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Photobooth.BoothNative;

public sealed class TemplateManager
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly string _assetsRoot;
    private readonly string _templatesRoot;
    private readonly string _framesRoot;
    private readonly string _overlaysRoot;
    private readonly string _stickersRoot;
    private readonly string _backgroundsRoot;
    private readonly string _effectsRoot;
    private readonly string _templateCatalogFile;
    private readonly string _effectCatalogFile;

    public TemplateManager()
    {
        _assetsRoot = Path.Combine(AppContext.BaseDirectory, "assets");
        _templatesRoot = Path.Combine(_assetsRoot, "templates");
        _framesRoot = Path.Combine(_assetsRoot, "frames");
        _overlaysRoot = Path.Combine(_assetsRoot, "overlays");
        _stickersRoot = Path.Combine(_assetsRoot, "stickers");
        _backgroundsRoot = Path.Combine(_assetsRoot, "backgrounds");
        _effectsRoot = Path.Combine(_assetsRoot, "effects");
        _templateCatalogFile = Path.Combine(_templatesRoot, "catalog.json");
        _effectCatalogFile = Path.Combine(_effectsRoot, "catalog.json");
    }

    public string AssetsRoot => _assetsRoot;
    public string TemplatesRoot => _templatesRoot;
    public string FramesRoot => _framesRoot;
    public string OverlaysRoot => _overlaysRoot;
    public string StickersRoot => _stickersRoot;
    public string BackgroundsRoot => _backgroundsRoot;
    public string EffectsRoot => _effectsRoot;

    public async Task InitializeAsync()
    {
        Directory.CreateDirectory(_assetsRoot);
        Directory.CreateDirectory(_templatesRoot);
        Directory.CreateDirectory(_framesRoot);
        Directory.CreateDirectory(_overlaysRoot);
        Directory.CreateDirectory(_stickersRoot);
        Directory.CreateDirectory(_backgroundsRoot);
        Directory.CreateDirectory(_effectsRoot);

        await EnsureStarterVisualAssetsAsync();

        await EnsureTemplateCatalogAsync();
        await EnsureEffectCatalogAsync();
    }

    public async Task<List<NativeTemplateRecord>> LoadTemplatesAsync()
    {
        await InitializeAsync();
        await using var stream = File.OpenRead(_templateCatalogFile);
        var catalog = await JsonSerializer.DeserializeAsync<NativeTemplateCatalog>(stream, JsonOptions) ?? new NativeTemplateCatalog();
        var defaults = BuildDefaultTemplates();
        var templates = catalog.Templates
            .Where(template => !string.IsNullOrWhiteSpace(template.Id))
            .Select(NormalizeTemplate)
            .OrderBy(template => template.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var fallback in defaults.Select(NormalizeTemplate))
        {
            if (templates.All(template => !string.Equals(template.Id, fallback.Id, StringComparison.OrdinalIgnoreCase)))
            {
                templates.Add(fallback);
            }
        }

        return templates.Count > 0 ? templates.OrderBy(template => template.Name, StringComparer.OrdinalIgnoreCase).ToList() : defaults;
    }

    public async Task<List<NativeEffectPresetRecord>> LoadEffectPresetsAsync()
    {
        await InitializeAsync();
        await using var stream = File.OpenRead(_effectCatalogFile);
        var catalog = await JsonSerializer.DeserializeAsync<NativeEffectPresetCatalog>(stream, JsonOptions) ?? new NativeEffectPresetCatalog();
        var presets = catalog.Presets
            .Where(preset => !string.IsNullOrWhiteSpace(preset.Id))
            .OrderBy(preset => preset.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return presets.Count > 0 ? presets : BuildDefaultEffectPresets();
    }

    private NativeTemplateRecord NormalizeTemplate(NativeTemplateRecord template)
    {
        template.Slots ??= new List<NativeTemplateSlotRecord>();
        template.TextBlocks ??= new List<NativeTemplateTextRecord>();
        template.DecorativeLayers ??= new List<NativeDecorativeLayerRecord>();
        template.Orientation = string.IsNullOrWhiteSpace(template.Orientation) ? "portrait" : template.Orientation;
        template.StyleFamily = string.IsNullOrWhiteSpace(template.StyleFamily) ? "clean-editorial" : template.StyleFamily;
        template.ExportWidth = template.ExportWidth <= 0 ? 1200 : template.ExportWidth;
        template.ExportHeight = template.ExportHeight <= 0 ? 1800 : template.ExportHeight;
        template.Dpi = template.Dpi <= 0 ? 300 : template.Dpi;
        template.BackgroundColorHex = string.IsNullOrWhiteSpace(template.BackgroundColorHex) ? "#f8f2e8" : template.BackgroundColorHex;
        template.PaperSize = string.IsNullOrWhiteSpace(template.PaperSize) ? "4x6" : template.PaperSize;

        foreach (var slot in template.Slots)
        {
            slot.FitMode = string.IsNullOrWhiteSpace(slot.FitMode) ? "cover" : slot.FitMode;
            slot.Label = string.IsNullOrWhiteSpace(slot.Label) ? slot.Id : slot.Label;
        }

        if (!string.IsNullOrWhiteSpace(template.BackgroundImagePath))
        {
            template.BackgroundImagePath = ResolveAssetPath(template.BackgroundImagePath);
        }

        if (!string.IsNullOrWhiteSpace(template.FinalOverlayPath))
        {
            template.FinalOverlayPath = ResolveAssetPath(template.FinalOverlayPath);
        }

        foreach (var layer in template.DecorativeLayers)
        {
            if (!string.IsNullOrWhiteSpace(layer.AssetPath))
            {
                layer.AssetPath = ResolveAssetPath(layer.AssetPath);
            }
        }

        return template;
    }

    private string ResolveAssetPath(string assetPath)
    {
        if (Path.IsPathRooted(assetPath))
        {
            return assetPath;
        }

        return Path.GetFullPath(Path.Combine(_assetsRoot, assetPath));
    }

    private List<NativeTemplateRecord> BuildDefaultTemplates()
    {
        return
        [
            new NativeTemplateRecord
            {
                Id = "grid-4x6-2x3",
                Name = "4x6 Portrait Grid 2x3",
                PaperSize = "4x6",
                Description = "Six-photo 4x6 print with two columns and three rows.",
                Orientation = "portrait",
                StyleFamily = "clean-editorial",
                ExportWidth = 1200,
                ExportHeight = 1800,
                Dpi = 300,
                BackgroundColorHex = "#f7efe1",
                BackgroundImagePath = "backgrounds/editorial-cream.png",
                DefaultFrameId = "frame-soft-gold",
                Slots = BuildUniformGridSlots(2, 3, 84, 84, 468, 504, 30),
                TextBlocks =
                [
                    new NativeTemplateTextRecord
                    {
                        Id = "timestamp",
                        Text = "Photobooth",
                        X = 72,
                        Y = 1710,
                        Width = 1056,
                        FontSize = 34,
                        Alignment = "center",
                        UseSessionTimestamp = true
                    }
                ]
            },
            new NativeTemplateRecord
            {
                Id = "grid-4x6-2x2",
                Name = "4x6 Portrait Grid 2x2",
                PaperSize = "4x6",
                Description = "Four-photo 4x6 layout with balanced spacing.",
                Orientation = "portrait",
                StyleFamily = "clean-editorial",
                ExportWidth = 1200,
                ExportHeight = 1800,
                Dpi = 300,
                BackgroundColorHex = "#faf4ea",
                BackgroundImagePath = "backgrounds/editorial-cream.png",
                DefaultFrameId = "frame-white-classic",
                Slots = BuildUniformGridSlots(2, 2, 96, 120, 456, 684, 42)
            },
            new NativeTemplateRecord
            {
                Id = "strip-1x4",
                Name = "1x4 Strip",
                PaperSize = "2x6",
                Description = "Classic four-photo vertical strip.",
                Orientation = "portrait",
                StyleFamily = "clean-editorial",
                ExportWidth = 900,
                ExportHeight = 1800,
                Dpi = 300,
                BackgroundColorHex = "#fdf6ef",
                BackgroundImagePath = "backgrounds/editorial-cream.png",
                DefaultFrameId = "frame-soft-gold",
                Slots = BuildUniformGridSlots(1, 4, 78, 70, 744, 360, 36)
            },
            new NativeTemplateRecord
            {
                Id = "grid-4x6-2x4",
                Name = "4x6 Grid 2x4",
                PaperSize = "4x6",
                Description = "Eight-photo 4x6 layout with two columns and four rows.",
                Orientation = "portrait",
                StyleFamily = "clean-editorial",
                ExportWidth = 1200,
                ExportHeight = 1800,
                Dpi = 300,
                BackgroundColorHex = "#faf3e7",
                BackgroundImagePath = "backgrounds/editorial-cream.png",
                DefaultFrameId = "frame-soft-gold",
                Slots = BuildUniformGridSlots(2, 4, 96, 66, 438, 390, 36)
            },
            new NativeTemplateRecord
            {
                Id = "collage-editorial-feature",
                Name = "Editorial Collage",
                PaperSize = "4x6",
                Description = "Event-style editorial collage with uneven image blocks.",
                Orientation = "portrait",
                StyleFamily = "clean-editorial",
                ExportWidth = 1200,
                ExportHeight = 1800,
                Dpi = 300,
                BackgroundColorHex = "#f9f5ef",
                BackgroundImagePath = "backgrounds/editorial-cream.png",
                DefaultFrameId = "frame-white-classic",
                Slots =
                [
                    new NativeTemplateSlotRecord { Id = "slot-1", Label = "1", X = 84, Y = 92, Width = 1032, Height = 620, BorderRadius = 18, FitMode = "cover" },
                    new NativeTemplateSlotRecord { Id = "slot-2", Label = "2", X = 84, Y = 752, Width = 498, Height = 458, BorderRadius = 18, FitMode = "cover" },
                    new NativeTemplateSlotRecord { Id = "slot-3", Label = "3", X = 618, Y = 752, Width = 498, Height = 458, BorderRadius = 18, FitMode = "cover" },
                    new NativeTemplateSlotRecord { Id = "slot-4", Label = "4", X = 84, Y = 1248, Width = 1032, Height = 450, BorderRadius = 18, FitMode = "cover" }
                ],
                TextBlocks =
                [
                    new NativeTemplateTextRecord
                    {
                        Id = "footer",
                        Text = "Event memories",
                        X = 84,
                        Y = 1722,
                        Width = 1032,
                        FontSize = 26,
                        Alignment = "center",
                        ColorHex = "#6f6257"
                    }
                ]
            },
            new NativeTemplateRecord
            {
                Id = "strip-scrapbook-cute",
                Name = "Scrapbook Cute Strip",
                PaperSize = "2x6",
                Description = "Decorative scrapbook strip with playful stickers and torn-paper mood.",
                Orientation = "portrait",
                StyleFamily = "scrapbook-cute",
                ExportWidth = 900,
                ExportHeight = 1800,
                Dpi = 300,
                BackgroundColorHex = "#fff6f4",
                BackgroundImagePath = "backgrounds/scrapbook-paper.png",
                DefaultFrameId = "frame-peach-strip",
                FinalOverlayPath = "overlays/torn-edge-strip.png",
                Slots = BuildUniformGridSlots(1, 4, 96, 122, 708, 314, 54),
                DecorativeLayers =
                [
                    new NativeDecorativeLayerRecord { Id = "tape-top", AssetPath = "stickers/tape-top.png", X = 292, Y = 24, Width = 320, Height = 116, Opacity = 0.96d },
                    new NativeDecorativeLayerRecord { Id = "heart", AssetPath = "stickers/heart-coral.png", X = 706, Y = 52, Width = 98, Height = 98, Rotation = -6, Opacity = 0.92d },
                    new NativeDecorativeLayerRecord { Id = "star", AssetPath = "stickers/star-yellow.png", X = 96, Y = 1582, Width = 84, Height = 84, Rotation = -10, Opacity = 0.95d },
                    new NativeDecorativeLayerRecord { Id = "doodle", AssetPath = "stickers/doodle-spark.png", X = 612, Y = 1580, Width = 146, Height = 120, Rotation = 8, Opacity = 0.88d }
                ],
                TextBlocks =
                [
                    new NativeTemplateTextRecord
                    {
                        Id = "caption",
                        Text = "best day ever",
                        X = 204,
                        Y = 1650,
                        Width = 492,
                        FontSize = 36,
                        Alignment = "center",
                        FontFamily = "Segoe Script",
                        ColorHex = "#6b4b4b"
                    }
                ]
            }
        ];
    }

    private List<NativeEffectPresetRecord> BuildDefaultEffectPresets()
    {
        return
        [
            new NativeEffectPresetRecord
            {
                Id = "clean-modern",
                Name = "Clean Modern",
                Description = "Balanced color with minimal processing for event-style collages.",
                Brightness = 0.04d,
                Contrast = 0.05d,
                WarmTone = 0.02d,
                GrainAmount = 0d,
                SoftBeautyBlend = 0.1d
            },
            new NativeEffectPresetRecord
            {
                Id = "bw-classic-strip",
                Name = "B&W Classic Strip",
                Description = "Classic monochrome photobooth strip look.",
                BlackAndWhite = true,
                Contrast = 0.18d,
                Brightness = 0.02d,
                GrainAmount = 0.03d
            },
            new NativeEffectPresetRecord
            {
                Id = "vintage-soft",
                Name = "Vintage Soft",
                Description = "Warm vintage tone with light grain and soft contrast.",
                VintageTone = true,
                Brightness = 0.03d,
                Contrast = 0.08d,
                WarmTone = 0.1d,
                GrainAmount = 0.04d,
                SoftBeautyBlend = 0.08d
            },
            new NativeEffectPresetRecord
            {
                Id = "scrapbook-cute",
                Name = "Scrapbook Cute",
                Description = "Playful warm scrapbook look with gentle softness.",
                Brightness = 0.06d,
                Contrast = 0.04d,
                WarmTone = 0.12d,
                GrainAmount = 0.02d,
                SoftBeautyBlend = 0.14d
            },
            new NativeEffectPresetRecord
            {
                Id = "overlay-only",
                Name = "Overlay Only",
                Description = "Keep original colors and apply only layout/decorations.",
                OverlayOnly = true
            }
        ];
    }

    private async Task EnsureTemplateCatalogAsync()
    {
        var defaults = BuildDefaultTemplates();
        NativeTemplateCatalog catalog;
        if (File.Exists(_templateCatalogFile))
        {
            await using var readStream = File.OpenRead(_templateCatalogFile);
            catalog = await JsonSerializer.DeserializeAsync<NativeTemplateCatalog>(readStream, JsonOptions) ?? new NativeTemplateCatalog();
        }
        else
        {
            catalog = new NativeTemplateCatalog();
        }

        catalog.Templates ??= new List<NativeTemplateRecord>();
        var changed = false;
        foreach (var fallback in defaults)
        {
            if (catalog.Templates.All(template => !string.Equals(template.Id, fallback.Id, StringComparison.OrdinalIgnoreCase)))
            {
                catalog.Templates.Add(fallback);
                changed = true;
            }
        }

        if (!File.Exists(_templateCatalogFile) || changed)
        {
            await using var writeStream = File.Create(_templateCatalogFile);
            await JsonSerializer.SerializeAsync(writeStream, catalog, JsonOptions);
        }
    }

    private async Task EnsureEffectCatalogAsync()
    {
        var defaults = BuildDefaultEffectPresets();
        NativeEffectPresetCatalog catalog;
        if (File.Exists(_effectCatalogFile))
        {
            await using var readStream = File.OpenRead(_effectCatalogFile);
            catalog = await JsonSerializer.DeserializeAsync<NativeEffectPresetCatalog>(readStream, JsonOptions) ?? new NativeEffectPresetCatalog();
        }
        else
        {
            catalog = new NativeEffectPresetCatalog();
        }

        catalog.Presets ??= new List<NativeEffectPresetRecord>();
        var changed = false;
        foreach (var fallback in defaults)
        {
            if (catalog.Presets.All(preset => !string.Equals(preset.Id, fallback.Id, StringComparison.OrdinalIgnoreCase)))
            {
                catalog.Presets.Add(fallback);
                changed = true;
            }
        }

        if (!File.Exists(_effectCatalogFile) || changed)
        {
            await using var writeStream = File.Create(_effectCatalogFile);
            await JsonSerializer.SerializeAsync(writeStream, catalog, JsonOptions);
        }
    }

    private List<NativeTemplateSlotRecord> BuildUniformGridSlots(int columns, int rows, double startX, double startY, double slotWidth, double slotHeight, double gap)
    {
        var slots = new List<NativeTemplateSlotRecord>();
        var index = 1;
        for (var row = 0; row < rows; row++)
        {
            for (var column = 0; column < columns; column++)
            {
                slots.Add(new NativeTemplateSlotRecord
                {
                    Id = $"slot-{index}",
                    Label = index.ToString(CultureInfo.InvariantCulture),
                    X = startX + (column * (slotWidth + gap)),
                    Y = startY + (row * (slotHeight + gap)),
                    Width = slotWidth,
                    Height = slotHeight,
                    Rotation = 0,
                    BorderRadius = 22,
                    FitMode = "cover"
                });
                index++;
            }
        }

        return slots;
    }

    private async Task EnsureStarterVisualAssetsAsync()
    {
        await Task.Run(() =>
        {
            CreatePaperBackgroundIfMissing(Path.Combine(_backgroundsRoot, "editorial-cream.png"), Color.FromRgb(248, 244, 237), Color.FromRgb(238, 231, 221), 1200, 1800);
            CreatePaperBackgroundIfMissing(Path.Combine(_backgroundsRoot, "scrapbook-paper.png"), Color.FromRgb(255, 245, 244), Color.FromRgb(247, 225, 220), 900, 1800);

            CreateTornEdgeOverlayIfMissing(Path.Combine(_overlaysRoot, "torn-edge-strip.png"), 900, 1800);

            CreateTapeStickerIfMissing(Path.Combine(_stickersRoot, "tape-top.png"), 320, 116);
            CreateHeartStickerIfMissing(Path.Combine(_stickersRoot, "heart-coral.png"), 98, 98);
            CreateStarStickerIfMissing(Path.Combine(_stickersRoot, "star-yellow.png"), 84, 84);
            CreateSparkStickerIfMissing(Path.Combine(_stickersRoot, "doodle-spark.png"), 146, 120);
        });
    }

    private void CreatePaperBackgroundIfMissing(string path, Color topColor, Color bottomColor, int width, int height)
    {
        if (File.Exists(path))
        {
            return;
        }

        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
        {
            var gradient = new LinearGradientBrush(topColor, bottomColor, new Point(0, 0), new Point(0, 1));
            dc.DrawRectangle(gradient, null, new Rect(0, 0, width, height));

            var speckPen = new Pen(new SolidColorBrush(Color.FromArgb(18, 164, 132, 120)), 1);
            for (var y = 24; y < height; y += 80)
            {
                dc.DrawLine(speckPen, new Point(20, y), new Point(width - 20, y + 12));
            }
        }

        SaveVisual(path, visual, width, height);
    }

    private void CreateTornEdgeOverlayIfMissing(string path, int width, int height)
    {
        if (File.Exists(path))
        {
            return;
        }

        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
        {
            var pen = new Pen(new SolidColorBrush(Color.FromArgb(156, 255, 255, 255)), 18);
            dc.DrawRoundedRectangle(null, pen, new Rect(36, 36, width - 72, height - 72), 34, 34);

            var brush = new SolidColorBrush(Color.FromArgb(82, 255, 255, 255));
            for (var index = 0; index < 7; index++)
            {
                var x = 24 + (index * 124);
                dc.DrawEllipse(brush, null, new Point(x, 34), 48, 22);
                dc.DrawEllipse(brush, null, new Point(width - x, height - 34), 46, 20);
            }
        }

        SaveVisual(path, visual, width, height);
    }

    private void CreateTapeStickerIfMissing(string path, int width, int height)
    {
        if (File.Exists(path))
        {
            return;
        }

        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
        {
            dc.PushTransform(new RotateTransform(-4, width / 2d, height / 2d));
            dc.DrawRoundedRectangle(
                new SolidColorBrush(Color.FromArgb(186, 255, 245, 210)),
                new Pen(new SolidColorBrush(Color.FromArgb(110, 242, 230, 191)), 2),
                new Rect(12, 16, width - 24, height - 32),
                16,
                16);
            dc.Pop();
        }

        SaveVisual(path, visual, width, height);
    }

    private void CreateHeartStickerIfMissing(string path, int width, int height)
    {
        if (File.Exists(path))
        {
            return;
        }

        var geometry = Geometry.Parse("M 49,84 C 10,60 0,28 24,12 C 38,2 56,14 49,28 C 42,14 60,2 74,12 C 98,28 88,60 49,84 Z");
        SaveStickerGeometry(path, geometry, width, height, Color.FromRgb(249, 137, 146));
    }

    private void CreateStarStickerIfMissing(string path, int width, int height)
    {
        if (File.Exists(path))
        {
            return;
        }

        var geometry = Geometry.Parse("M 42,4 L 52,28 L 80,30 L 58,47 L 66,76 L 42,60 L 18,76 L 26,47 L 4,30 L 32,28 Z");
        SaveStickerGeometry(path, geometry, width, height, Color.FromRgb(255, 212, 102));
    }

    private void CreateSparkStickerIfMissing(string path, int width, int height)
    {
        if (File.Exists(path))
        {
            return;
        }

        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
        {
            var pen = new Pen(new SolidColorBrush(Color.FromArgb(210, 248, 163, 181)), 8)
            {
                StartLineCap = PenLineCap.Round,
                EndLineCap = PenLineCap.Round
            };
            dc.DrawLine(pen, new Point(18, 56), new Point(128, 56));
            dc.DrawLine(pen, new Point(74, 10), new Point(74, 106));
            dc.DrawLine(new Pen(new SolidColorBrush(Color.FromArgb(180, 248, 163, 181)), 6), new Point(28, 20), new Point(120, 92));
            dc.DrawLine(new Pen(new SolidColorBrush(Color.FromArgb(180, 248, 163, 181)), 6), new Point(120, 20), new Point(28, 92));
        }

        SaveVisual(path, visual, width, height);
    }

    private void SaveStickerGeometry(string path, Geometry geometry, int width, int height, Color fillColor)
    {
        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
        {
            dc.PushTransform(new ScaleTransform(width / 98d, height / 98d));
            dc.DrawGeometry(new SolidColorBrush(fillColor), new Pen(new SolidColorBrush(Color.FromArgb(120, 255, 255, 255)), 4), geometry);
            dc.Pop();
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
