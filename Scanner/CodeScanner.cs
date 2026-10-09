using System.Drawing;
using OpenCvSharp;
using Scanner.Result;
using ZXing;
using ZXing.Common;
using ZXing.Multi;
using ZXing.Windows.Compatibility;
using Image = System.Drawing.Image;

namespace Scanner;

[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public class CodeScanner
{
    private readonly Mat _originalImage;
    private readonly Mat _grayImage = new();
    
    public CodeScanner(byte[] imageData)
    {
        _originalImage = Cv2.ImDecode(imageData, ImreadModes.Color);
        Cv2.CvtColor(_originalImage, _grayImage, ColorConversionCodes.BGR2GRAY);
    }
    
    public void FindCodes(ref ScanResult result)
    {
        var bm = (Bitmap)Image.FromStream(_originalImage.ToMemoryStream());
        var findings = FindQrCodesUsingZxing(bm);
        ParseDecodedText(ref result, findings);
        if (IsValidScanResult(result))
        {
            return;
        }
        
        var opencvFindings = FindQrCodesUsingOpenCv(_grayImage);
        ParseDecodedText(ref result, opencvFindings);
    }
    
    private static string[] FindQrCodesUsingZxing(Bitmap image)
    {
        var binary = new BinaryBitmap(new HybridBinarizer(new BitmapLuminanceSource(image)));
        
        var baseReader = new MultiFormatReader()
        {
            Hints = new Dictionary<DecodeHintType, object>
            {
                { DecodeHintType.POSSIBLE_FORMATS, new List<BarcodeFormat> { BarcodeFormat.QR_CODE } },
                { DecodeHintType.TRY_HARDER, true },
            }
        };

        var reader = new GenericMultipleBarcodeReader(new ByQuadrantReader(baseReader));
        var result = reader.decodeMultiple(binary);
        
        return (result ?? []).Select(r => r?.Text ?? string.Empty).ToArray();
    }
    
    private static string[] FindQrCodesUsingOpenCv(Mat image)
    {
        QRCodeDetector detector = new();
        Point2f[] points;
        
        detector.DetectMulti(image, out points);
        if (points.Length == 0)
        {
            return [];
        }
        
        detector.DecodeMulti(image, points, out var decodedTexts);
        return decodedTexts.Select(r => r ?? string.Empty).ToArray();
    }
    
    private static void ParseDecodedText(ref ScanResult result, string[] texts)
    {
        foreach (var text in texts)
        {
            if (string.IsNullOrEmpty(text)) continue;
            if (IsUuid(text))
            {
                var uuid = Guid.ParseExact(text, "N");
                result.ExamCode = new Rollback<Guid?>(uuid);
            }
            else if (IsNeptunCode(text))
            {
                result.UserCode = new Rollback<string?>(text);
            }
        }
    }
    
    public static bool IsValidScanResult(ScanResult result)
    {
        var examCode = result.ExamCode.Get();
        var userCode = result.UserCode.Get();
        return examCode != Guid.Empty && !string.IsNullOrEmpty(userCode);
    }

    public static bool IsUuid(string text)
        => Guid.TryParseExact(text, "N", out _);

    public static bool IsNeptunCode(string text)
        => text.Length == 6 && text.All(char.IsLetterOrDigit);
}