namespace Scanner.Codes;

public readonly struct CodePack
{
    public readonly ExamCodeNotNull ExamCode;
    public readonly UserCode UserCode;
    
    public readonly ExamCode InitialExamCode;
    public readonly UserCode InitialUserCode;

    private CodePack(ExamCodeNotNull examCode, UserCode userCode, ExamCode initialExamCode, UserCode initialUserCode)
    {
        ExamCode = examCode;
        UserCode = userCode;
        InitialExamCode = initialExamCode;
        InitialUserCode = initialUserCode;
    }

    internal static CodePack CreateAutoHistory(ExamCodeNotNull examCode, UserCode userCode)
    {
        return new CodePack(examCode, userCode, examCode.Inner, userCode);
    }

    internal static CodePack Create(ExamCodeNotNull examCode, UserCode userCode, ExamCode initialExamCode, UserCode initialUserCode)
    {
        return new CodePack(examCode, userCode, initialExamCode, initialUserCode);
    }
}