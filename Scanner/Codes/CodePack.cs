using Scanner.Managing;

namespace Scanner.Codes;

public readonly struct CodePack
{
    public readonly Valid<ExamCode> ExamCode;
    public readonly ExamCode InitialExamCode;
    
    public readonly UserCode UserCode;
    public readonly UserCode InitialUserCode;

    private CodePack(Valid<ExamCode> examCode, UserCode userCode, ExamCode initialExamCode, UserCode initialUserCode)
    {
        ExamCode = examCode;
        UserCode = userCode;
        InitialExamCode = initialExamCode;
        InitialUserCode = initialUserCode;
    }

    internal static CodePack CreateAutoHistory(Valid<ExamCode> examCode, UserCode userCode)
    {
        return new CodePack(examCode, userCode, examCode, userCode);
    }

    internal static CodePack Create(Valid<ExamCode> examCode, UserCode userCode, ExamCode initialExamCode, UserCode initialUserCode)
    {
        return new CodePack(examCode, userCode, initialExamCode, initialUserCode);
    }
}