using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Photobooth.BoothNative;

public sealed class LivePhotoPreviewService
{
    private readonly BeautyProcessor _beautyProcessor = new();

    public async Task<BitmapSource?> RenderPreviewAsync(
        string imagePath,
        NativeBeautyLevel beautyLevel,
        NativeEffectPresetRecord? effectPreset,
        NativePreviewStickerKind stickerKind,
        NativePreviewMaskMode maskMode,
        int maxWidth = 1600)
    {
        if (string.IsNullOrWhiteSpace(imagePath) || !File.Exists(imagePath))
        {
            return null;
        }

        await _beautyProcessor.InitializeAsync();
        var baseBitmap = LoadBitmap(imagePath);
        var processed = _beautyProcessor.ProcessBitmap(baseBitmap, beautyLevel, effectPreset);
        return RenderDecorated(processed, stickerKind, maskMode, maxWidth);
    }

    private BitmapSource RenderDecorated(BitmapSource baseBitmap, NativePreviewStickerKind stickerKind, NativePreviewMaskMode maskMode, int maxWidth)
    {
        var targetWidth = Math.Max(1, Math.Min(maxWidth, baseBitmap.PixelWidth));
        var scale = targetWidth / (double)baseBitmap.PixelWidth;
        var targetHeight = Math.Max(1, (int)Math.Round(baseBitmap.PixelHeight * scale));

        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
        {
            dc.DrawImage(baseBitmap, new Rect(0, 0, targetWidth, targetHeight));
            DrawMask(dc, maskMode, targetWidth, targetHeight);
            DrawSticker(dc, stickerKind, targetWidth, targetHeight);
        }

        var bitmap = new RenderTargetBitmap(targetWidth, targetHeight, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        bitmap.Freeze();
        return bitmap;
    }

    private static void DrawMask(DrawingContext dc, NativePreviewMaskMode mode, int width, int height)
    {
        switch (mode)
        {
            case NativePreviewMaskMode.LeftHalf:
                dc.DrawRectangle(new SolidColorBrush(Color.FromArgb(78, 255, 210, 210)), null, new Rect(0, 0, width / 2d, height));
                break;
            case NativePreviewMaskMode.RightHalf:
                dc.DrawRectangle(new SolidColorBrush(Color.FromArgb(78, 210, 220, 255)), null, new Rect(width / 2d, 0, width / 2d, height));
                break;
            case NativePreviewMaskMode.CenterSpotlight:
                var overlay = new SolidColorBrush(Color.FromArgb(95, 0, 0, 0));
                dc.DrawRectangle(overlay, null, new Rect(0, 0, width, height));
                dc.PushClip(new EllipseGeometry(new Point(width / 2d, height / 2d), width * 0.24d, height * 0.28d));
                dc.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, width, height));
                dc.Pop();
                break;
        }
    }

    private static void DrawSticker(DrawingContext dc, NativePreviewStickerKind kind, int width, int height)
    {
        switch (kind)
        {
            case NativePreviewStickerKind.DogEars:
                DrawDogEars(dc, width, height);
                break;
            case NativePreviewStickerKind.PartyHat:
                DrawPartyHat(dc, width, height);
                break;
            case NativePreviewStickerKind.Hearts:
                DrawHearts(dc, width, height);
                break;
        }
    }

    private static void DrawDogEars(DrawingContext dc, int width, int height)
    {
        var brush = new SolidColorBrush(Color.FromArgb(220, 102, 66, 44));
        var inner = new SolidColorBrush(Color.FromArgb(220, 227, 177, 164));
        var leftOuter = CreateEarGeometry(width * 0.24d, height * 0.1d, width * 0.17d, height * 0.24d, -18);
        var rightOuter = CreateEarGeometry(width * 0.76d, height * 0.1d, width * 0.17d, height * 0.24d, 18);
        var leftInner = CreateEarGeometry(width * 0.24d, height * 0.14d, width * 0.09d, height * 0.16d, -18);
        var rightInner = CreateEarGeometry(width * 0.76d, height * 0.14d, width * 0.09d, height * 0.16d, 18);
        dc.DrawGeometry(brush, null, leftOuter);
        dc.DrawGeometry(brush, null, rightOuter);
        dc.DrawGeometry(inner, null, leftInner);
        dc.DrawGeometry(inner, null, rightInner);
    }

    private static void DrawPartyHat(DrawingContext dc, int width, int height)
    {
        var geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            ctx.BeginFigure(new Point(width * 0.5d, height * 0.03d), true, true);
            ctx.LineTo(new Point(width * 0.38d, height * 0.24d), true, false);
            ctx.LineTo(new Point(width * 0.62d, height * 0.24d), true, false);
        }
        geometry.Freeze();
        dc.DrawGeometry(new SolidColorBrush(Color.FromArgb(230, 255, 154, 77)), new Pen(Brushes.White, 4), geometry);
        dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(240, 255, 214, 64)), null, new Point(width * 0.5d, height * 0.03d), width * 0.025d, width * 0.025d);
    }

    private static void DrawHearts(DrawingContext dc, int width, int height)
    {
        DrawHeart(dc, width * 0.18d, height * 0.18d, 36);
        DrawHeart(dc, width * 0.82d, height * 0.22d, 28);
        DrawHeart(dc, width * 0.16d, height * 0.8d, 22);
    }

    private static void DrawHeart(DrawingContext dc, double cx, double cy, double size)
    {
        var geo = new StreamGeometry();
        using (var ctx = geo.Open())
        {
            ctx.BeginFigure(new Point(cx, cy + size * 0.3d), true, true);
            ctx.BezierTo(new Point(cx - size, cy - size * 0.2d), new Point(cx - size * 0.9d, cy - size), new Point(cx, cy - size * 0.35d), true, false);
            ctx.BezierTo(new Point(cx + size * 0.9d, cy - size), new Point(cx + size, cy - size * 0.2d), new Point(cx, cy + size * 0.9d), true, false);
        }
        geo.Freeze();
        dc.DrawGeometry(new SolidColorBrush(Color.FromArgb(215, 255, 120, 146)), null, geo);
    }

    private static Geometry CreateEarGeometry(double centerX, double centerY, double width, double height, double tilt)
    {
        var geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            ctx.BeginFigure(new Point(centerX, centerY), true, true);
            ctx.BezierTo(
                new Point(centerX - width * 0.55d, centerY + height * 0.25d),
                new Point(centerX - width * 0.48d, centerY + height),
                new Point(centerX, centerY + height),
                true,
                false);
            ctx.BezierTo(
                new Point(centerX + width * 0.48d, centerY + height),
                new Point(centerX + width * 0.55d, centerY + height * 0.25d),
                new Point(centerX, centerY),
                true,
                false);
        }

        geometry.Freeze();
        return new RotateTransform(tilt, centerX, centerY + height * 0.5d).TransformBounds(new Rect(centerX - width, centerY, width * 2, height)) == Rect.Empty
            ? geometry
            : geometry.CloneCurrentValue();
    }

    private static BitmapImage LoadBitmap(string path)
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
