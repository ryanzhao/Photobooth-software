using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Photobooth.BoothNative;

public sealed class CompositeRenderer
{
    private readonly BeautyProcessor _beautyProcessor = new();

    public RenderTargetBitmap RenderPreview(
        NativeTemplateRecord template,
        IReadOnlyList<NativePhotoRecord> photos,
        NativeFrameRecord? frame,
        NativeSessionRecord? session,
        NativeEffectPresetRecord? effectPreset,
        int previewWidth = 900,
        int previewHeight = 600)
    {
        var previewSize = FitSize(template.ExportWidth, template.ExportHeight, previewWidth, previewHeight);
        var targetWidth = Math.Max(1, (int)Math.Round(previewSize.Width));
        var targetHeight = Math.Max(1, (int)Math.Round(previewSize.Height));
        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
        {
            DrawComposition(dc, template, photos, frame, session, effectPreset, targetWidth, targetHeight, isPreview: true);
        }

        var bitmap = new RenderTargetBitmap(targetWidth, targetHeight, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        bitmap.Freeze();
        return bitmap;
    }

    public RenderTargetBitmap RenderGuideOverlay(
        NativeTemplateRecord template,
        IReadOnlyList<NativePhotoRecord> photos,
        NativeFrameRecord? frame,
        int targetWidth,
        int targetHeight)
    {
        targetWidth = Math.Max(1, targetWidth);
        targetHeight = Math.Max(1, targetHeight);

        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
        {
            DrawGuideOverlay(dc, template, photos, frame, targetWidth, targetHeight);
        }

        var bitmap = new RenderTargetBitmap(targetWidth, targetHeight, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        bitmap.Freeze();
        return bitmap;
    }

    public NativeRenderResult RenderFinal(
        NativeTemplateRecord template,
        IReadOnlyList<NativePhotoRecord> photos,
        NativeFrameRecord? frame,
        NativeSessionRecord session,
        NativeEffectPresetRecord? effectPreset)
    {
        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
        {
            DrawComposition(dc, template, photos, frame, session, effectPreset, template.ExportWidth, template.ExportHeight, isPreview: false);
        }

        var bitmap = new RenderTargetBitmap(template.ExportWidth, template.ExportHeight, template.Dpi, template.Dpi, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        bitmap.Freeze();

        Directory.CreateDirectory(session.FinalFolderPath);
        var baseName = $"{session.Id}_{template.Id}";
        var pngPath = Path.Combine(session.FinalFolderPath, $"{baseName}.png");
        var jpgPath = Path.Combine(session.FinalFolderPath, $"{baseName}.jpg");

        using (var pngStream = File.Create(pngPath))
        {
            var pngEncoder = new PngBitmapEncoder();
            pngEncoder.Frames.Add(BitmapFrame.Create(bitmap));
            pngEncoder.Save(pngStream);
        }

        using (var jpgStream = File.Create(jpgPath))
        {
            var jpgEncoder = new JpegBitmapEncoder { QualityLevel = 95 };
            jpgEncoder.Frames.Add(BitmapFrame.Create(bitmap));
            jpgEncoder.Save(jpgStream);
        }

        return new NativeRenderResult
        {
            PngPath = pngPath,
            JpgPath = jpgPath
        };
    }

    private void DrawComposition(
        DrawingContext dc,
        NativeTemplateRecord template,
        IReadOnlyList<NativePhotoRecord> photos,
        NativeFrameRecord? frame,
        NativeSessionRecord? session,
        NativeEffectPresetRecord? effectPreset,
        int canvasWidth,
        int canvasHeight,
        bool isPreview)
    {
        var background = ParseColor(template.BackgroundColorHex, Color.FromRgb(248, 242, 232));
        dc.DrawRectangle(new SolidColorBrush(background), null, new Rect(0, 0, canvasWidth, canvasHeight));

        if (!string.IsNullOrWhiteSpace(template.BackgroundImagePath) && File.Exists(template.BackgroundImagePath))
        {
            DrawImage(dc, template.BackgroundImagePath, new Rect(0, 0, canvasWidth, canvasHeight), 0, 0, fitMode: "cover");
        }

        var scaleX = canvasWidth / (double)template.ExportWidth;
        var scaleY = canvasHeight / (double)template.ExportHeight;

        for (var index = 0; index < template.Slots.Count; index++)
        {
            var slot = template.Slots[index];
            var rect = new Rect(slot.X * scaleX, slot.Y * scaleY, slot.Width * scaleX, slot.Height * scaleY);
            var photo = photos.FirstOrDefault(candidate => candidate.SlotIndex == index);
            if (photo is not null && File.Exists(photo.ProcessedFilePath))
            {
                DrawPhoto(dc, photo, rect, slot.BorderRadius, slot.Rotation, slot.FitMode, session, effectPreset);
            }
            else
            {
                DrawEmptySlot(dc, rect, slot.Label);
            }
        }

        foreach (var layer in template.DecorativeLayers)
        {
            if (isPreview || layer.ShowInPreview)
            {
                DrawDecorativeLayer(dc, layer, isPreview ? scaleX : 1d, isPreview ? scaleY : 1d);
            }
        }

        if (template.TextBlocks.Count > 0)
        {
            foreach (var textBlock in template.TextBlocks)
            {
                DrawTextBlock(dc, textBlock, isPreview ? scaleX : 1d, isPreview ? scaleY : 1d, session);
            }
        }

        if (!string.IsNullOrWhiteSpace(template.FinalOverlayPath) && File.Exists(template.FinalOverlayPath))
        {
            DrawImage(dc, template.FinalOverlayPath, new Rect(0, 0, canvasWidth, canvasHeight), 0, 0, fitMode: "stretch");
        }

        if (frame is not null && File.Exists(frame.OverlayPath))
        {
            if (!isPreview || frame.ApplyMode == NativeOverlayApplyMode.GuideAndFinal)
            {
                var opacity = isPreview ? frame.PreviewOpacity : 1d;
                dc.PushOpacity(opacity);
                DrawImage(dc, frame.OverlayPath, new Rect(0, 0, canvasWidth, canvasHeight), 0, 0, fitMode: "stretch");
                dc.Pop();
            }
        }
    }

    private void DrawGuideOverlay(
        DrawingContext dc,
        NativeTemplateRecord template,
        IReadOnlyList<NativePhotoRecord> photos,
        NativeFrameRecord? frame,
        int canvasWidth,
        int canvasHeight)
    {
        var layoutRect = ComputeFittedTemplateRect(canvasWidth, canvasHeight, template.ExportWidth, template.ExportHeight);
        var scaleX = layoutRect.Width / template.ExportWidth;
        var scaleY = layoutRect.Height / template.ExportHeight;

        foreach (var slot in template.Slots)
        {
            var rect = new Rect(
                layoutRect.X + (slot.X * scaleX),
                layoutRect.Y + (slot.Y * scaleY),
                slot.Width * scaleX,
                slot.Height * scaleY);
            var isFilled = photos.Any(photo => photo.SlotIndex == template.Slots.IndexOf(slot));
            DrawGuideSlot(dc, rect, slot.Label, isFilled);
        }

        if (frame is not null && frame.ApplyMode == NativeOverlayApplyMode.GuideAndFinal && File.Exists(frame.OverlayPath))
        {
            dc.PushOpacity(Math.Max(0.12d, Math.Min(0.85d, frame.PreviewOpacity)));
            DrawImage(dc, frame.OverlayPath, layoutRect, 0, 0, fitMode: "stretch");
            dc.Pop();
        }
    }

    private void DrawPhoto(
        DrawingContext dc,
        NativePhotoRecord photo,
        Rect targetRect,
        double borderRadius,
        double rotation,
        string fitMode,
        NativeSessionRecord? session,
        NativeEffectPresetRecord? effectPreset)
    {
        var path = photo.ProcessedFilePath;
        if (!File.Exists(path))
        {
            return;
        }

        var bitmap = LoadBitmap(path);
        var beautyLevel = session is not null && Enum.TryParse<NativeBeautyLevel>(session.SelectedBeautyLevel, true, out var level)
            ? level
            : NativeBeautyLevel.Off;
        var processed = _beautyProcessor.ProcessBitmap(bitmap, beautyLevel, effectPreset);
        dc.PushClip(new RectangleGeometry(targetRect, borderRadius, borderRadius));
        var centerX = targetRect.X + (targetRect.Width / 2d);
        var centerY = targetRect.Y + (targetRect.Height / 2d);
        var photoScale = double.IsFinite(photo.EditScale) ? Math.Max(0.4d, Math.Min(2.4d, photo.EditScale)) : 1d;
        var photoOffsetX = Math.Max(-0.6d, Math.Min(0.6d, photo.EditOffsetX));
        var photoOffsetY = Math.Max(-0.6d, Math.Min(0.6d, photo.EditOffsetY));
        var photoRotation = Math.Max(-90d, Math.Min(90d, photo.EditRotation));
        var totalRotation = rotation + photoRotation;

        dc.PushTransform(new RotateTransform(totalRotation, centerX, centerY));

        if (string.Equals(fitMode, "stretch", StringComparison.OrdinalIgnoreCase))
        {
            dc.DrawImage(processed, targetRect);
        }
        else
        {
            var imageRatio = processed.PixelWidth / (double)processed.PixelHeight;
            var targetRatio = targetRect.Width / targetRect.Height;
            Rect drawRect;

            if (string.Equals(fitMode, "contain", StringComparison.OrdinalIgnoreCase))
            {
                if (imageRatio > targetRatio)
                {
                    var height = targetRect.Width / imageRatio;
                    var width = targetRect.Width * photoScale;
                    var adjustedHeight = height * photoScale;
                    drawRect = new Rect(
                        targetRect.X + ((targetRect.Width - width) / 2d) + (photoOffsetX * targetRect.Width),
                        targetRect.Y + ((targetRect.Height - adjustedHeight) / 2d) + (photoOffsetY * targetRect.Height),
                        width,
                        adjustedHeight);
                }
                else
                {
                    var width = targetRect.Height * imageRatio;
                    var adjustedWidth = width * photoScale;
                    var height = targetRect.Height * photoScale;
                    drawRect = new Rect(
                        targetRect.X + ((targetRect.Width - adjustedWidth) / 2d) + (photoOffsetX * targetRect.Width),
                        targetRect.Y + ((targetRect.Height - height) / 2d) + (photoOffsetY * targetRect.Height),
                        adjustedWidth,
                        height);
                }
            }
            else
            {
                var scale = Math.Max(targetRect.Width / processed.PixelWidth, targetRect.Height / processed.PixelHeight);
                var width = processed.PixelWidth * scale;
                var height = processed.PixelHeight * scale;
                width *= photoScale;
                height *= photoScale;
                drawRect = new Rect(
                    targetRect.X + ((targetRect.Width - width) / 2d) + (photoOffsetX * targetRect.Width),
                    targetRect.Y + ((targetRect.Height - height) / 2d) + (photoOffsetY * targetRect.Height),
                    width,
                    height);
            }

            dc.DrawImage(processed, drawRect);
        }

        dc.Pop();
        dc.Pop();
    }

    private void DrawImage(DrawingContext dc, string path, Rect targetRect, double borderRadius, double rotation, string fitMode)
    {
        var bitmap = LoadBitmap(path);

        dc.PushClip(new RectangleGeometry(targetRect, borderRadius, borderRadius));
        dc.PushTransform(new RotateTransform(rotation, targetRect.X + (targetRect.Width / 2d), targetRect.Y + (targetRect.Height / 2d)));

        if (string.Equals(fitMode, "stretch", StringComparison.OrdinalIgnoreCase))
        {
            dc.DrawImage(bitmap, targetRect);
        }
        else
        {
            var imageRatio = bitmap.PixelWidth / (double)bitmap.PixelHeight;
            var targetRatio = targetRect.Width / targetRect.Height;
            Rect drawRect;

            if (string.Equals(fitMode, "contain", StringComparison.OrdinalIgnoreCase))
            {
                if (imageRatio > targetRatio)
                {
                    var height = targetRect.Width / imageRatio;
                    drawRect = new Rect(targetRect.X, targetRect.Y + ((targetRect.Height - height) / 2d), targetRect.Width, height);
                }
                else
                {
                    var width = targetRect.Height * imageRatio;
                    drawRect = new Rect(targetRect.X + ((targetRect.Width - width) / 2d), targetRect.Y, width, targetRect.Height);
                }
            }
            else
            {
                var scale = Math.Max(targetRect.Width / bitmap.PixelWidth, targetRect.Height / bitmap.PixelHeight);
                var width = bitmap.PixelWidth * scale;
                var height = bitmap.PixelHeight * scale;
                drawRect = new Rect(
                    targetRect.X + ((targetRect.Width - width) / 2d),
                    targetRect.Y + ((targetRect.Height - height) / 2d),
                    width,
                    height);
            }

            dc.DrawImage(bitmap, drawRect);
        }

        dc.Pop();
        dc.Pop();
    }

    private void DrawDecorativeLayer(DrawingContext dc, NativeDecorativeLayerRecord layer, double scaleX, double scaleY)
    {
        if (string.IsNullOrWhiteSpace(layer.AssetPath) || !File.Exists(layer.AssetPath))
        {
            return;
        }

        var rect = new Rect(layer.X * scaleX, layer.Y * scaleY, layer.Width * scaleX, layer.Height * scaleY);
        dc.PushOpacity(Math.Max(0d, Math.Min(1d, layer.Opacity)));
        DrawImage(dc, layer.AssetPath, rect, 0, layer.Rotation, "stretch");
        dc.Pop();
    }

    private void DrawTextBlock(DrawingContext dc, NativeTemplateTextRecord textBlock, double scaleX, double scaleY, NativeSessionRecord? session)
    {
        var text = textBlock.UseSessionTimestamp && session is not null
            ? $"{textBlock.Text} · {session.CreatedAt:yyyy-MM-dd HH:mm}"
            : textBlock.Text;

        var formatted = new FormattedText(
            text,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface(textBlock.FontFamily),
            Math.Max(14, textBlock.FontSize * Math.Min(scaleX, scaleY)),
            new SolidColorBrush(ParseColor(textBlock.ColorHex, Color.FromRgb(91, 66, 46))),
            1.0d)
        {
            MaxTextWidth = textBlock.Width * scaleX,
            TextAlignment = textBlock.Alignment switch
            {
                "center" => TextAlignment.Center,
                "right" => TextAlignment.Right,
                _ => TextAlignment.Left
            }
        };

        dc.DrawText(formatted, new Point(textBlock.X * scaleX, textBlock.Y * scaleY));
    }

    private void DrawEmptySlot(DrawingContext dc, Rect rect, string label)
    {
        var fill = new SolidColorBrush(Color.FromRgb(255, 250, 244));
        var border = new Pen(new SolidColorBrush(Color.FromRgb(215, 190, 158)), 2) { DashStyle = DashStyles.Dash };
        dc.DrawRoundedRectangle(fill, border, rect, 18, 18);

        var text = new FormattedText(
            label,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface("Segoe UI Semibold"),
            Math.Max(16, rect.Width / 7d),
            new SolidColorBrush(Color.FromRgb(190, 160, 125)),
            1.0d);
        dc.DrawText(text, new Point(rect.X + ((rect.Width - text.Width) / 2d), rect.Y + ((rect.Height - text.Height) / 2d)));
    }

    private void DrawGuideSlot(DrawingContext dc, Rect rect, string label, bool isFilled)
    {
        var fill = isFilled
            ? new SolidColorBrush(Color.FromArgb(42, 255, 255, 255))
            : new SolidColorBrush(Color.FromArgb(10, 255, 255, 255));
        var borderColor = isFilled
            ? Color.FromArgb(210, 255, 244, 228)
            : Color.FromArgb(220, 255, 229, 196);
        var border = new Pen(new SolidColorBrush(borderColor), isFilled ? 3.2d : 2.2d)
        {
            DashStyle = isFilled ? DashStyles.Solid : DashStyles.Dash
        };
        dc.DrawRoundedRectangle(fill, border, rect, 18, 18);

        if (string.IsNullOrWhiteSpace(label))
        {
            return;
        }

        var text = new FormattedText(
            label,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface("Segoe UI Semibold"),
            Math.Max(14, Math.Min(rect.Width, rect.Height) / 6.5d),
            new SolidColorBrush(isFilled ? Color.FromArgb(210, 255, 248, 240) : Color.FromArgb(220, 255, 236, 214)),
            1.0d);
        dc.DrawText(text, new Point(rect.X + ((rect.Width - text.Width) / 2d), rect.Y + ((rect.Height - text.Height) / 2d)));
    }

    private static Size FitSize(int sourceWidth, int sourceHeight, int maxWidth, int maxHeight)
    {
        if (sourceWidth <= 0 || sourceHeight <= 0)
        {
            return new Size(Math.Max(1, maxWidth), Math.Max(1, maxHeight));
        }

        var widthScale = Math.Max(1d, maxWidth) / sourceWidth;
        var heightScale = Math.Max(1d, maxHeight) / sourceHeight;
        var scale = Math.Min(widthScale, heightScale);
        return new Size(sourceWidth * scale, sourceHeight * scale);
    }

    private static Rect ComputeFittedTemplateRect(int canvasWidth, int canvasHeight, int templateWidth, int templateHeight)
    {
        var fitted = FitSize(templateWidth, templateHeight, canvasWidth, canvasHeight);
        return new Rect(
            (canvasWidth - fitted.Width) / 2d,
            (canvasHeight - fitted.Height) / 2d,
            fitted.Width,
            fitted.Height);
    }

    private Color ParseColor(string? colorHex, Color fallback)
    {
        if (string.IsNullOrWhiteSpace(colorHex))
        {
            return fallback;
        }

        try
        {
            return (Color)ColorConverter.ConvertFromString(colorHex);
        }
        catch
        {
            return fallback;
        }
    }

    private BitmapImage LoadBitmap(string path)
    {
        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.UriSource = new Uri(path, UriKind.Absolute);
        bitmap.EndInit();
        bitmap.Freeze();
        return bitmap;
    }
}
