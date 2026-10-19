using System.Drawing;

namespace Scanner.Managing;

public readonly struct MissingDataState
{
    public readonly Bitmap PageBitmap;
    public readonly string AutoUserCode;
    public readonly string AutoExamCode;
    public readonly bool IsExamAlreadySolved;

    internal MissingDataState(Bitmap pageBitmap, UserCode autoUserCode, ExamCode autoExamCode, bool solved)
    {
        PageBitmap = pageBitmap;
        AutoUserCode = autoUserCode.Code;
        AutoExamCode = autoExamCode.Code;
        IsExamAlreadySolved = solved;
    }
}

public struct MissingData
{
    internal Guid OwnerKey;
    
    public ExamCode ExamCode;
    public UserCode UserCode;
    
    public readonly MissingDataState State;
    
    private MissingData(ExamCode examCode, UserCode userCode, Bitmap bitmap, bool solved, Guid ownerKey)
    {
        OwnerKey = ownerKey;
        ExamCode = examCode;
        UserCode = userCode;
        State = new MissingDataState(bitmap, userCode, examCode, solved);
    }
    
    internal static MissingData Init(ExamCode examCode, UserCode userCode, Bitmap bitmap, bool solved, out Guid ownerKey)
    {
        // The returned Guid is guaranteed to not equal Guid.Empty.
        // https://learn.microsoft.com/en-us/dotnet/api/system.guid.newguid?view=net-8.0
        ownerKey = Guid.NewGuid();
        return new MissingData(examCode, userCode, bitmap, solved, ownerKey);
    }
    
    /// Destroys this object and returns a FilledData in its stead
    public FilledData Validate(Valid<ExamCode> examCode, Valid<UserCode> userCode)
    {
        var entry = new FilledData(examCode, userCode, OwnerKey);
        OwnerKey = Guid.Empty;
        return entry;
    }
}

public readonly struct FilledData
{
    internal readonly Guid OwnerKey;
    
    public readonly Valid<ExamCode> ExamCode;
    public readonly Valid<UserCode> UserCode;

    internal FilledData(Valid<ExamCode> examCode, Valid<UserCode> userCode, Guid ownerKey)
    {
        OwnerKey = ownerKey;
        ExamCode = examCode;
        UserCode = userCode;
    }
}