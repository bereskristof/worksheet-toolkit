using System.Drawing;
using OpenCvSharp;
using Scanner.Managing;
using ZXing;
using ZXing.Common;
using ZXing.Multi;
using ZXing.Windows.Compatibility;
using Image = System.Drawing.Image;

namespace Scanner.Codes;

internal struct FoundCodes
{
    public Valid<ExamCode>? ExamCode;
    public Valid<UserCode>? UserCode;

    public UserCode GetUserCode() => UserCode ?? Managing.UserCode.Empty;
    public ExamCode GetExamCode() => ExamCode ?? Managing.ExamCode.Empty;
    
    /// Returns true if both fields are set to a valid value.
    public bool IsValid() => ExamCode is not null && UserCode is not null;
    
    public bool IsExamValid() => ExamCode is not null;

    /// Join 2 separate results, keeping anything possible,
    /// with a preference for the calling object.
    internal FoundCodes Merge(FoundCodes other)
    {
        return new FoundCodes
        {
            ExamCode = ExamCode ?? other.ExamCode,
            UserCode = UserCode ?? other.UserCode,
        };
    }
}

[System.Runtime.Versioning.SupportedOSPlatform("windows")]
internal class CodeScanner
{
    private readonly Mat _originalImage;
    private readonly Mat _grayImage = new();
    
    public CodeScanner(byte[] imageData)
    {
        _originalImage = Cv2.ImDecode(imageData, ImreadModes.Color);
        Cv2.CvtColor(_originalImage, _grayImage, ColorConversionCodes.BGR2GRAY);
    }
    
    public FoundCodes FindCodes()
    {
        var bm = (Bitmap)Image.FromStream(_originalImage.ToMemoryStream());
        var findings = FindQrCodesUsingZxing(bm);
        var foundCodes = ParseDecodedText(findings);
        if (foundCodes.IsValid())
        {
            return foundCodes;
        }
        
        var opencvFindings = FindQrCodesUsingOpenCv(_grayImage);
        return ParseDecodedText(opencvFindings).Merge(foundCodes);
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
        
        return [.. (result ?? []).Select(r => r?.Text ?? string.Empty)];
    }
    
    private static string[] FindQrCodesUsingOpenCv(Mat image)
    {
        QRCodeDetector detector = new();

        detector.DetectMulti(image, out var points);
        if (points.Length == 0)
        {
            return [];
        }
        
        detector.DecodeMulti(image, points, out var decodedTexts);
        return [.. decodedTexts.Select(r => r ?? string.Empty)];
    }
    
    private static FoundCodes ParseDecodedText(string[] texts)
    {
        Valid<ExamCode>? examCode = null;
        Valid<UserCode>? userCode = null;
        foreach (var text in texts)
        {
            if (string.IsNullOrEmpty(text)) continue;
            if (UserCode.Create(text).TryValidate() is { } foundUserCode)
            {
                userCode = foundUserCode;
            }
            else if (ExamCode.Create(text).TryValidate() is { } foundExamCode)
            {
                examCode = foundExamCode;
            }
        }

        return new FoundCodes
        {
            ExamCode = examCode,
            UserCode = userCode,
        };
    }
}