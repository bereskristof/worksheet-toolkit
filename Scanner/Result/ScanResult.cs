namespace Scanner.Result;

public struct ScanResult
{
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
    
    public const int SuccessScoreCount = 24;
    
    public State CurrentState;
    
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
}