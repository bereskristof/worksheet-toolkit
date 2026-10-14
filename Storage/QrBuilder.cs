using System.Drawing;
using System.Drawing.Imaging;
using QRCoder;

namespace Storage;

[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public class QrBuilder
{
    private const int QrCellSize = 25;
    
    // 300 PPI, although due to downscaling, it may look worse.
    private const int Width = 437;
    private const int Height = 437;
    
    private readonly Image _image;
    
    public QrBuilder(string key)
    {
        using var codeGenerator = new QRCodeGenerator();
        using var qrCodeData = codeGenerator.CreateQrCode(key, QRCodeGenerator.ECCLevel.H);
        using var pngCode = new PngByteQRCode(qrCodeData);
        
        byte[] qrCodePng = pngCode.GetGraphic(QrCellSize, Color.Black, Color.White);
        Image qrCodeImage = Image.FromStream(new MemoryStream(qrCodePng));
        _image = new Bitmap(qrCodeImage, Width, Height);
    }
    
    public void Save(string filePath)
    {
        _image.Save(filePath, ImageFormat.Png);
    }
}