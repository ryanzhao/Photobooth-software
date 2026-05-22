using System.Windows;
using System.Windows.Media.Imaging;

namespace Photobooth.BoothNative;

public partial class FullscreenPreviewWindow : Window
{
    public FullscreenPreviewWindow()
    {
        InitializeComponent();
    }

    public void SetPreview(BitmapSource? bitmap, string caption)
    {
        PreviewImage.Source = bitmap;
        CaptionText.Text = caption;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
