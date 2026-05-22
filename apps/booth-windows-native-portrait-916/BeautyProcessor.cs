using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Photobooth.BoothNative;

public sealed class BeautyProcessor
{
    private readonly string _cascadeDirectory;

    public BeautyProcessor()
    {
        _cascadeDirectory = Path.Combine(AppContext.BaseDirectory, "assets", "opencv");
    }

    public async Task InitializeAsync()
    {
        Directory.CreateDirectory(_cascadeDirectory);
        await TryCopyBuiltInCascadeAsync("haarcascade_frontalface_default.xml");
    }

    public async Task<BitmapSource> ProcessAsync(string rawFilePath, NativeBeautyLevel beautyLevel, NativeEffectPresetRecord? preset = null)
    {
        await InitializeAsync();
        var bitmap = LoadBitmap(rawFilePath);
        return await Task.Run(() => ProcessBitmap(bitmap, beautyLevel, preset));
    }

    public async Task<string> ProcessAndSaveAsync(
        string rawFilePath,
        string processedFilePath,
        NativeBeautyLevel beautyLevel,
        NativeEffectPresetRecord? preset = null)
    {
        var processed = await ProcessAsync(rawFilePath, beautyLevel, preset);
        Directory.CreateDirectory(Path.GetDirectoryName(processedFilePath)!);
        using var stream = File.Create(processedFilePath);
        var encoder = new JpegBitmapEncoder { QualityLevel = 94 };
        encoder.Frames.Add(BitmapFrame.Create(processed));
        encoder.Save(stream);
        return processedFilePath;
    }

    public BitmapSource ProcessBitmap(BitmapSource bitmap, NativeBeautyLevel beautyLevel, NativeEffectPresetRecord? preset = null)
    {
        var width = bitmap.PixelWidth;
        var height = bitmap.PixelHeight;
        var baseBitmap = EnsurePbgra32(bitmap);
        var profile = NativeBeautyProfile.FromLevel(beautyLevel);

        if (preset?.OverlayOnly == true && beautyLevel == NativeBeautyLevel.Off)
        {
            return baseBitmap;
        }

        var softenedBase = ApplyBeauty(baseBitmap, profile);
        return ApplyEffectPreset(softenedBase, preset);
    }

    private BitmapSource ApplyBeauty(BitmapSource bitmap, NativeBeautyProfile profile)
    {
        if (profile.Level == NativeBeautyLevel.Off)
        {
            return bitmap;
        }

        var width = bitmap.PixelWidth;
        var height = bitmap.PixelHeight;
        var softBitmap = profile.SkinSmoothingStrength > 0
            ? RenderBlurredBitmap(bitmap, width, height, GetBlurRadius(profile.SkinSmoothingStrength))
            : bitmap;
        var blemishBitmap = profile.BlemishSofteningStrength > 0
            ? RenderBlurredBitmap(bitmap, width, height, 1.5d)
            : bitmap;
        var visual = new DrawingVisual();

        using (var dc = visual.RenderOpen())
        {
            dc.DrawImage(bitmap, new Rect(0, 0, width, height));

            if (profile.SkinSmoothingStrength > 0)
            {
                dc.PushOpacity(Math.Min(0.28d, profile.SkinSmoothingStrength));
                dc.DrawImage(softBitmap, new Rect(0, 0, width, height));
                dc.Pop();
            }

            var faceRegion = new Rect(width * 0.18d, height * 0.12d, width * 0.64d, height * 0.48d);
            var liftOpacity = profile.FaceLiftStrength > 0 ? Math.Min(0.18d, profile.FaceLiftStrength + 0.03d) : 0d;
            if (liftOpacity > 0)
            {
                var liftBrush = new RadialGradientBrush(
                    Color.FromArgb((byte)(255 * liftOpacity), 255, 244, 232),
                    Color.FromArgb(0, 255, 244, 232))
                {
                    RadiusX = 0.72d,
                    RadiusY = 0.68d,
                    Center = new Point(0.5d, 0.45d),
                    GradientOrigin = new Point(0.5d, 0.45d)
                };
                dc.DrawRectangle(liftBrush, null, faceRegion);
            }

            var contrastOpacity = Math.Min(0.11d, profile.ContrastStrength + 0.02d);
            if (contrastOpacity > 0)
            {
                dc.DrawRectangle(new SolidColorBrush(Color.FromArgb((byte)(255 * contrastOpacity), 245, 229, 208)), null, new Rect(0, 0, width, height));
            }

            var warmthOpacity = Math.Min(0.08d, profile.WarmToneStrength);
            if (warmthOpacity > 0)
            {
                dc.DrawRectangle(new SolidColorBrush(Color.FromArgb((byte)(255 * warmthOpacity), 255, 221, 193)), null, new Rect(0, 0, width, height));
            }

            var blemishOpacity = Math.Min(0.12d, profile.BlemishSofteningStrength);
            if (blemishOpacity > 0)
            {
                dc.PushOpacity(blemishOpacity);
                dc.PushClip(new RectangleGeometry(faceRegion));
                dc.DrawImage(blemishBitmap, new Rect(0, 0, width, height));
                dc.Pop();
                dc.Pop();
            }
        }

        var render = new RenderTargetBitmap(width, height, 300, 300, PixelFormats.Pbgra32);
        render.Render(visual);
        render.Freeze();
        return render;
    }

    private BitmapSource ApplyEffectPreset(BitmapSource bitmap, NativeEffectPresetRecord? preset)
    {
        if (preset is null)
        {
            return bitmap;
        }

        var sourceBitmap = preset.BlackAndWhite ? new FormatConvertedBitmap(bitmap, PixelFormats.Gray8, BitmapPalettes.Gray256, 0) : bitmap;
        if (sourceBitmap.CanFreeze)
        {
            sourceBitmap.Freeze();
        }
        var width = sourceBitmap.PixelWidth;
        var height = sourceBitmap.PixelHeight;
        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
        {
            dc.DrawImage(sourceBitmap, new Rect(0, 0, width, height));

            if (preset.VintageTone)
            {
                dc.DrawRectangle(new SolidColorBrush(Color.FromArgb(46, 214, 177, 132)), null, new Rect(0, 0, width, height));
            }

            if (preset.Brightness > 0)
            {
                var alpha = (byte)Math.Min(255, Math.Round(preset.Brightness * 180d));
                dc.DrawRectangle(new SolidColorBrush(Color.FromArgb(alpha, 255, 248, 241)), null, new Rect(0, 0, width, height));
            }

            if (preset.Contrast > 0)
            {
                var alpha = (byte)Math.Min(255, Math.Round(preset.Contrast * 120d));
                dc.DrawRectangle(new SolidColorBrush(Color.FromArgb(alpha, 245, 228, 205)), null, new Rect(0, 0, width, height));
            }

            if (preset.WarmTone > 0)
            {
                var alpha = (byte)Math.Min(255, Math.Round(preset.WarmTone * 160d));
                dc.DrawRectangle(new SolidColorBrush(Color.FromArgb(alpha, 255, 218, 192)), null, new Rect(0, 0, width, height));
            }

            if (preset.GrainAmount > 0)
            {
                DrawLightGrain(dc, width, height, preset.GrainAmount);
            }
        }

        var render = new RenderTargetBitmap(width, height, 300, 300, PixelFormats.Pbgra32);
        render.Render(visual);
        render.Freeze();
        return render;
    }

    private void DrawLightGrain(DrawingContext dc, int width, int height, double amount)
    {
        var count = Math.Max(80, (int)Math.Round(width * height * amount / 1200d));
        var random = new Random((width * 31) ^ (height * 17) ^ (int)(amount * 1000));
        var brush = new SolidColorBrush(Color.FromArgb(20, 105, 88, 72));
        for (var index = 0; index < count; index++)
        {
            var x = random.NextDouble() * width;
            var y = random.NextDouble() * height;
            var size = 0.8d + (random.NextDouble() * 1.8d);
            dc.DrawRectangle(brush, null, new Rect(x, y, size, size));
        }
    }

    private static double GetBlurRadius(double strength) => strength switch
    {
        <= 0 => 0d,
        < 0.2d => 1.5d,
        < 0.34d => 2.5d,
        _ => 3.8d
    };

    private static BitmapSource EnsurePbgra32(BitmapSource bitmap)
    {
        if (bitmap.Format == PixelFormats.Pbgra32)
        {
            return bitmap;
        }

        var converted = new FormatConvertedBitmap();
        converted.BeginInit();
        converted.Source = bitmap;
        converted.DestinationFormat = PixelFormats.Pbgra32;
        converted.EndInit();
        converted.Freeze();
        return converted;
    }

    private static BitmapSource RenderBlurredBitmap(BitmapSource source, int width, int height, double radius)
    {
        if (Application.Current?.Dispatcher is { } dispatcher)
        {
            return dispatcher.Invoke(() => RenderBlurredBitmapOnSta(source, width, height, radius));
        }

        BitmapSource? result = null;
        Exception? error = null;
        var thread = new Thread(() =>
        {
            try
            {
                result = RenderBlurredBitmapOnSta(source, width, height, radius);
            }
            catch (Exception ex)
            {
                error = ex;
            }
            finally
            {
                Dispatcher.CurrentDispatcher.InvokeShutdown();
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (error is not null)
        {
            throw error;
        }

        return result ?? source;
    }

    private static BitmapSource RenderBlurredBitmapOnSta(BitmapSource source, int width, int height, double radius)
    {
        var image = new System.Windows.Controls.Image
        {
            Width = width,
            Height = height,
            Source = source,
            Stretch = Stretch.Fill,
            Effect = new BlurEffect
            {
                Radius = radius,
                RenderingBias = RenderingBias.Quality
            }
        };

        image.Measure(new Size(width, height));
        image.Arrange(new Rect(0, 0, width, height));
        image.UpdateLayout();

        var render = new RenderTargetBitmap(width, height, 300, 300, PixelFormats.Pbgra32);
        render.Render(image);
        render.Freeze();
        return render;
    }

    private async Task TryCopyBuiltInCascadeAsync(string fileName)
    {
        var targetPath = Path.Combine(_cascadeDirectory, fileName);
        if (File.Exists(targetPath))
        {
            return;
        }

        var candidate = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Photoboof7", "utils", "misc", "haarcascades", fileName);
        candidate = Path.GetFullPath(candidate);
        if (File.Exists(candidate))
        {
            await Task.Run(() => File.Copy(candidate, targetPath, overwrite: false));
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
