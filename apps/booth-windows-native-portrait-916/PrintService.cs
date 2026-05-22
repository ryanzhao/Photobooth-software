using System.Diagnostics;
using System.IO;
using System.Windows;

namespace Photobooth.BoothNative;

public sealed class PrintService
{
    public void OpenForPrintPreview(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("The final composite image does not exist.", path);
        }

        Process.Start(new ProcessStartInfo(path)
        {
            UseShellExecute = true,
            Verb = "print"
        });
    }

    public void OpenFile(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("The requested file does not exist.", path);
        }

        Process.Start(new ProcessStartInfo(path)
        {
            UseShellExecute = true
        });
    }
}
