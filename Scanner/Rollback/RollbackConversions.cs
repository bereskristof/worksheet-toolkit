using Scanner.Result;

namespace Scanner.Rollback;

public static class RollbackConversions
{
    public static Rollback<ScanResult.IssueFlags> ToRollback(this ScanResult.IssueFlags flag) => new(flag);
    
    public static Rollback<ExamCode> ToRollback(this ExamCode code) => new(code);
    
    public static Rollback<string?> ToNullableRollback(this string str) => new(str);
    
    public static Rollback<int?> ToNullableRollback(this int num) => new(num);
    
    public static Rollback<ExamCode> GetNullExamCode() => new(new ExamCode());
    
    public static Rollback<string?> GetNullString() => new(null);
    
    public static Rollback<int?> GetNullInt() => new(null);
}