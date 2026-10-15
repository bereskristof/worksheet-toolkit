using System.Drawing;

namespace Scanner.Codes;

/// An object to fill in missing data user data, i.e.: Exam & Neptun codes.
/// Owned by its parent StepGetCodes, it is runtime checked on return to be correct. 
public struct MissingDataEntry : IStepCodesReturnable
{
    internal Guid OwnerKey;
    
    internal readonly ExamCode InitialExamCode;
    internal readonly UserCode InitialUserCode;
    
    public ExamCode? ExamCode;
    public UserCode? UserCode;

    /// Kept only to make displaying the page easy,
    /// but is otherwise not required.
    public readonly Bitmap PageBitmap;

    private MissingDataEntry(ExamCode? examCode, UserCode? userCode, Bitmap page, Guid ownerKey)
    {
        OwnerKey = ownerKey;
        PageBitmap = page;
        ExamCode = examCode;
        UserCode = userCode;
        InitialExamCode = examCode ?? Codes.ExamCode.Empty;
        InitialUserCode = userCode ?? Codes.UserCode.Empty;
    }
    
    internal static MissingDataEntry Init(ExamCode? examCode, UserCode? userCode, Bitmap page, out Guid ownerKey)
    {
        // The returned Guid is guaranteed to not equal Guid.Empty.
        // https://learn.microsoft.com/en-us/dotnet/api/system.guid.newguid?view=net-8.0
        ownerKey = Guid.NewGuid();
        return new MissingDataEntry(examCode, userCode, page, ownerKey);
    }

    /// Verifies that all required data is filled.
    /// If so, a DataEntry is returned, and this object is destroyed.
    public DataEntry? TryValidate()
    {
        if (OwnerKey == Guid.Empty)
            return null;
        if (ExamCode is not { } examCode || UserCode is not { } userCode)
            return null;
        if (ExamCodeNotNull.Validate(examCode) is not { } validExamCode)
            return null;
        var entry = new DataEntry(validExamCode, userCode, InitialExamCode, InitialUserCode, OwnerKey);
        OwnerKey = Guid.Empty;
        return entry;
    }
}

public struct DataEntry
{
    internal Guid OwnerKey;
    
    internal readonly ExamCode InitialExamCode;
    internal readonly UserCode InitialUserCode;
    
    internal ExamCodeNotNull ExamCode;
    internal UserCode UserCode;

    internal DataEntry(ExamCodeNotNull examCode, UserCode userCode, ExamCode initialExamCode, UserCode initialUserCode, Guid ownerKey)
    {
        OwnerKey = ownerKey;
        ExamCode = examCode;
        UserCode = userCode;
        InitialExamCode = initialExamCode;
        InitialUserCode = initialUserCode;
    }
}