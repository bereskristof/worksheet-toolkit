using System.Diagnostics;
using System.Drawing;
using System.Runtime.Versioning;
using Scanner.Bubbles;
using Scanner.Error;
using Scanner.Managing;

namespace Scanner.Codes;

public class StepCodes : IStepCodes
{
    private readonly Bitmap _bitmap;
    private Guid _dataEntryVerify = Guid.Empty;

    private byte[] _image = [];
    private ExamCode _initialExamCode = ExamCode.Empty;
    private UserCode _initialUserCode = UserCode.Empty;

    internal StepCodes(Bitmap bitmap)
    {
        _bitmap = bitmap;
    }
    
    public StepCodesReturnable GetCodes()
    {
        try
        {
            var (foundCodes, imageBytes) = FindCodesFromImage();
            _image = imageBytes;
            _initialUserCode = foundCodes.GetUserCode();
            _initialExamCode = foundCodes.GetExamCode();
            return foundCodes.IsExamValid() switch
            {
                true => new StepCodesBubbles(CodePack.CreateAutoHistory(
                    foundCodes.ExamCode ?? throw new UnreachableException(),
                    foundCodes.GetUserCode()),
                    imageBytes, _bitmap),
                false => MissingData.Init(foundCodes.GetExamCode(), foundCodes.GetUserCode(), _bitmap, false, out _dataEntryVerify),
            };
        }
        catch (Exception e)
        {
            return new StepCodesError(e, UserCode.Empty);
        }
    }

    public IStepCodesBubbles GetCodes(FilledData dataEntry)
    {
        if (dataEntry.OwnerKey != _dataEntryVerify)
            throw new InvalidDataException("StepWithDataEntry: The provided MissingDataEntry does not match the entry that was given!");
        
        return new StepCodesBubbles(CodePack.Create(
            dataEntry.ExamCode,
            dataEntry.UserCode,
            _initialExamCode,
            _initialUserCode),
            _image, _bitmap);
    }

    public IStepCodesBubbles GetCodes(MissingData missingDataEntry)
    {
        if (missingDataEntry.OwnerKey != _dataEntryVerify)
            throw new InvalidDataException("StepSkipDataEntry: The provided MissingDataEntry does not match the entry that was given!");
        if (missingDataEntry.ExamCode.TryValidate() is not { } validCode)
            return new StepCodesError(new InvalidDataException("StepSkipDataEntry: Missing exam code"), missingDataEntry.UserCode);

        return new StepCodesBubbles(CodePack.Create(
            validCode,
            missingDataEntry.UserCode,
            _initialExamCode,
            _initialUserCode),
            _image, _bitmap);
    }

    private Tuple<FoundCodes, byte[]> FindCodesFromImage()
    {
        #pragma warning disable CA1416
        return OperatingSystem.IsWindows() switch
        {
            true => FindCodesFromImageWindows(),
            false => throw new PlatformNotSupportedException("FindCodesFromImage: Only available on Windows!"),
        };
        #pragma warning restore CA1416
    }

    [SupportedOSPlatform("windows")]
    private Tuple<FoundCodes, byte[]> FindCodesFromImageWindows()
    {
        var imageConverter = new ImageConverter();
        var imageData = (byte[])(imageConverter.ConvertTo(_bitmap, typeof(byte[])) ?? throw new InvalidOperationException("FindCodesFromImageWindows: Got an empty array"));
        var qrScanner = new CodeScanner(imageData);
        return new Tuple<FoundCodes, byte[]>(qrScanner.FindCodes(), imageData);
    }
}