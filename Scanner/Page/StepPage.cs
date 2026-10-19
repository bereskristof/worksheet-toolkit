using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Docnet.Core.Readers;
using Scanner.Codes;
using Scanner.Error;
using Scanner.Managing;

namespace Scanner.Page;

public class StepPage : IStepPage
{
    internal StepPage() {}

    public IStepCodes GetPage(IDocReader reader, int pageNumber)
    {
        try
        {
            var bitmap = GetPageAsBitmap(reader, pageNumber);
            return new StepCodes(bitmap);
        }
        catch (Exception e)
        {
            return new StepCodesError(e, UserCode.Empty);
        }
    }
    
    private static Bitmap GetPageAsBitmap(IDocReader reader, int pageNumber)
    {
        #pragma warning disable CA1416
        return OperatingSystem.IsWindows() switch
        {
            true => GetPageAsBitmapWindows(reader, pageNumber),
            false => throw new PlatformNotSupportedException("GetPageAsBitmap: Only available on Windows!"),
        };
        #pragma warning restore CA1416
    }

    [SupportedOSPlatform("windows")]
    private static Bitmap GetPageAsBitmapWindows(IDocReader reader, int pageNumber)
    {
        using var pageReader = reader.GetPageReader(pageNumber);
        var page = pageReader.GetImage();
            
        var width = pageReader.GetPageWidth();
        var height = pageReader.GetPageHeight();
            
        var bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            
        var bmpData = bmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
        Marshal.Copy(page, 0, bmpData.Scan0, page.Length);
        bmp.UnlockBits(bmpData);
            
        var stream = new MemoryStream();
        bmp.Save(stream, ImageFormat.Bmp);
        return bmp;
    }
}