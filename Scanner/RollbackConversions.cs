using Scanner;
using Scanner.Result;

namespace Interface.Solve;

public static class RollbackConversions
{
    public static Rollback<ScanResult.IssueFlags> ToRollback(this ScanResult.IssueFlags flag) => new(flag);
    
    public static Rollback<Guid?> ToNullableRollback(this Guid guid) => new(guid);
    
    public static Rollback<string?> ToNullableRollback(this string str) => new(str);
    
    public static Rollback<int?> ToNullableRollback(this int num) => new(num);
    
    public static Rollback<Guid?> GetNullGuid() => new(null);
    
    public static Rollback<string?> GetNullString() => new(null);
    
    public static Rollback<int?> GetNullInt() => new(null);
}