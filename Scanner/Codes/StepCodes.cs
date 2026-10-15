using System.Diagnostics;
using System.Drawing;
using System.Runtime.Versioning;
using Scanner.Bubbles;
using Scanner.Error;

namespace Scanner.Codes;

public class StepCodes : IStepCodes
{
    private readonly Bitmap _bitmap;
    private Guid _dataEntryVerify = Guid.Empty;

    private byte[] _image = [];

    internal StepCodes(Bitmap bitmap)
    {
        _bitmap = bitmap;
    }
    
    public IStepCodesReturnable GetCodes()
    {
        try
        {
            var (foundCodes, imageBytes) = FindCodesFromImage();
            _image = imageBytes;
            return foundCodes.IsValid() switch
            {
                true => new StepCodesBubbles(CodePack.CreateAutoHistory(
                    foundCodes.ExamCode ?? throw new UnreachableException(),
                    foundCodes.UserCode ?? throw new UnreachableException()),
                    imageBytes),
                false => MissingDataEntry.Init(foundCodes.ExamCode?.Inner ?? ExamCode.Empty, foundCodes.UserCode, _bitmap, out _dataEntryVerify),
            };
        }
        catch (Exception e)
        {
            return new StepCodesError(e);
        }
    }

    public IStepCodesBubbles GetCodesDataEntry(DataEntry dataEntry)
    {
        if (dataEntry.OwnerKey != _dataEntryVerify)
            throw new InvalidDataException("StepWithDataEntry: The provided MissingDataEntry does not match the entry that was given!");
        
        return new StepCodesBubbles(CodePack.Create(
            dataEntry.ExamCode,
            dataEntry.UserCode,
            dataEntry.InitialExamCode,
            dataEntry.InitialUserCode),
            _image);
    }

    public IStepCodesBubbles GetCodesSkipDataEntry(MissingDataEntry missingDataEntry)
    {
        if (missingDataEntry.OwnerKey != _dataEntryVerify)
            throw new InvalidDataException("StepSkipDataEntry: The provided MissingDataEntry does not match the entry that was given!");
        if (missingDataEntry.ExamCode is not { } notNullCode || ExamCodeNotNull.Validate(notNullCode) is not { } validCode)
            return new StepCodesError(new InvalidDataException("StepSkipDataEntry: Missing exam code"));

        return new StepCodesBubbles(CodePack.Create(
            validCode,
            missingDataEntry.UserCode ?? UserCode.Empty,
            missingDataEntry.InitialExamCode,
            missingDataEntry.InitialUserCode),
            _image);
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