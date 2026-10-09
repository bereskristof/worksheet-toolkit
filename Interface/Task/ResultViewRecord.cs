using Scanner;
using Storage.Sheet;

namespace Interface.Task;

public class ResultViewRecord
{
    enum ScanResultHuman
    {
        Ok,
        Partial,
        Error,
    }
    
    /// Maximum number of items in `Points`
    private const int PointsArraySize = (int)SheetTreeInfo.MaximumNumberOfQuestions;
    /// Score to have a successful test.
    private const int SuccessfulMinimumScore = ScanResult.SuccessScoreCount; 
    private const string UnknownString = "?";
    
    public int Page { get; set; }
    public ScanResult.State State { get; set; }
    public string StateString => GetScanResultHuman() switch
    {
        ScanResultHuman.Ok => Resources.Lang.Solve_StateOk,
        ScanResultHuman.Partial => Resources.Lang.Solve_StateWarning,
        _ => Resources.Lang.Solve_StateError,
    };
    public bool MajorError => GetScanResultHuman() == ScanResultHuman.Error;
    public bool MinorError => GetScanResultHuman() == ScanResultHuman.Partial;
    
    public string UserCode { get; set; }
    public string ExamCode { get; set; }
    
    public int TotalPoints => Points.Select(v => v ?? 0).Sum();
    public string Result => (TotalPoints >= SuccessfulMinimumScore)
        ? Resources.Lang.Export_ResultSuccess
        : Resources.Lang.Export_ResultFailure;
    
    public int?[] Points { get; init; }
    public int TaskCount => Points.Count(x => x != null);

    public ResultViewRecord(int page, ScanResult sourceResult)
    {
        Page = page;
        State = sourceResult.CurrentState;
        UserCode = sourceResult.UserCode ?? UnknownString;
        ExamCode = sourceResult.ExamCode?.ToString() ?? UnknownString;
        int?[] points = [.. sourceResult.Results.Select(r => (int?)(r.Points ?? 0))];
        Array.Resize(ref points, PointsArraySize);
        Points = points;
    }

    private ScanResultHuman GetScanResultHuman()
    {
        return State switch
        {
            ScanResult.State.Completed => ScanResultHuman.Ok,
            ScanResult.State.CompletedWithManualCorrection => ScanResultHuman.Ok,
            ScanResult.State.MissingUserCode => ScanResultHuman.Partial,
            _ => ScanResultHuman.Error
        };
    }
}