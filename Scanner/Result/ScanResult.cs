using Scanner.Codes;
using Scanner.Rollback;

namespace Scanner.Result;

/// A possible result of the scanning process.
/// Means no fatal error occured, and the results could be obtained.
public struct ScanResult : IScanResult
{
    public const int SuccessScoreCount = 24;
    
    public Rollback<ExamCode> ExamCode;
    public Rollback<UserCode> UserCode;
    
    public QuestionResult[] Results;
    
    public int? FinalPoints => Results.Sum(r => r.Points.Get());

    internal ScanResult(Rollback<ExamCode> examCode, Rollback<UserCode> userCode, QuestionResult[] results)
    {
        ExamCode = examCode;
        UserCode = userCode;
        Results = results;
    }

    public bool IsModified()
    {
        var rollbacks = new List<IRollback>
        {
            ExamCode,
            UserCode
        };
        rollbacks.AddRange(Results.Select(result => result.Points).Cast<IRollback>());
        return rollbacks.Any(rollback => rollback.IsModified());
    }

    public void Reset()
    {
        ExamCode.Reset();
        UserCode.Reset();
        var max = Results.Length;
        for (var i = 0; i < max; i++) // `QuestionResult` is a struct, you can't use a foreach here.
        {
            Results[i].Points.Reset();
        }
    }
}