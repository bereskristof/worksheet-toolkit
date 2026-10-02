using System.ComponentModel;

namespace Scanner.Result;

public struct ScanResult
{
    [Obsolete("Superseeded by `IssueFlags` & `MissingFieldFlags`", error: true)]
    public enum State
    {
        Unknown,
        Completed,
        CompletedWithManualCorrection,
        MissingExamCode,
        MissingUserCode,
        MissingTaskFromDatabase,
        UnreliableDataFromDatabase,
        MarkerDetectionError,
        ManuallyCorrected,
        UnexpectedException,
    }

    [Flags]
    public enum IssueFlags
    {
        None = 0,
        MissingExamCode = 1,
        MissingUserCode = 1 << 1,
        MissingTaskFromDatabase = 1 << 2,
        UnreliableDataFromDatabase = 1 << 3,
        MarkerDetectionError = 1 << 4,
        ManuallyCorrected = 1 << 5,
        UnexpectedException = 1 << 6,
    }

    [Flags]
    public enum MissingFieldFlags
    {
        None = 0,
        MissingExamCode = 1,
        MissingUserCode = 2,
    }
    
    public const int SuccessScoreCount = 24;
    
    public Rollback<IssueFlags> Issues;
    
    public Rollback<Guid?> ExamCode;
    public Rollback<string?> UserCode;
    
    public int? FinalPoints => Results.Sum(r => r.Points.Get() ?? 0);
    
    public QuestionResult[] Results;
    
    public static double CalculateConfidence(double bestFillPercent, double nextBestRatio)
    {
        const double v = 15;
        var fillConfidence = double.Min(1, double.Abs(v * bestFillPercent - v * QuestionResult.MinFillConfidence));
        var ratioConfidence = double.Min(1, double.Abs(v * nextBestRatio - v * QuestionResult.MinDeltaConfidence));
        var totalConfidence = ratioConfidence * fillConfidence;
        return totalConfidence;
    }

    public MissingFieldFlags GetMissingFields()
    {
        var flags = MissingFieldFlags.None;
        flags |= (ExamCode.Get() is null) ? MissingFieldFlags.MissingExamCode : MissingFieldFlags.None;
        flags |= (UserCode.Get() is null) ? MissingFieldFlags.MissingUserCode : MissingFieldFlags.None;
        return flags;
    }
}