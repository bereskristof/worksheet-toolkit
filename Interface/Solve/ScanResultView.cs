using System.Globalization;
using System.Text;
using Scanner;
using Scanner.Result;

namespace Interface.Solve;

public struct LocaleSpecifics
{
    public CultureInfo Culture;
    public string DecimalPoint;
    public string ListSeparator;
    public string SuccessText;
    public string FailText;
}

public class ScanResultView(int page, ScanResult sourceResult)
{
    enum ScanResultHuman
    {
        Ok,
        Partial,
        Error,
    }
    
    /// Score to have a successful test.
    private const int SuccessfulMinimumScore = ScanResult.SuccessScoreCount;
    private const string UnknownString = "?";

    public ScanResult ScanResult = sourceResult;
    public int Page { get; init; } = page;

    public ScanResult.State State => ScanResult.CurrentState;
    public string StateString => GetScanResultHuman() switch
    {
        ScanResultHuman.Ok => Resources.Lang.Solve_StateOk,
        ScanResultHuman.Partial => Resources.Lang.Solve_StateWarning,
        _ => Resources.Lang.Solve_StateError,
    };
    public bool MajorError => GetScanResultHuman() == ScanResultHuman.Error;
    public bool MinorError => GetScanResultHuman() == ScanResultHuman.Partial;
    
    public string UserCode => ScanResult.UserCode.Get() ?? UnknownString;
    public string ExamCode => ScanResult.ExamCode.Get()?.ToString() ?? UnknownString;
    
    public int TotalPoints => Points.Get().Sum();
    public int TaskCount => Points.Get().Length;
    public string Result => (TotalPoints >= SuccessfulMinimumScore)
        ? Resources.Lang.Export_ResultSuccess
        : Resources.Lang.Export_ResultFailure;
    
    public ScanResultViewPointAccess Points { get; init; } = new(sourceResult);

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
    
    public DualAutoSeparatedStringBuilder ToSheetCsv(int pageIndex, LocaleSpecifics localeSpecifics)
    {
        var sb = new DualAutoSeparatedStringBuilder(localeSpecifics.ListSeparator, (pageIndex + 1).ToString(), State.ToString());
        sb.Append(UserCode, "");
        sb.Append(TotalPoints.ToString(), "");
        sb.Append(TotalPoints >= SuccessfulMinimumScore ? localeSpecifics.SuccessText : localeSpecifics.FailText, "");
        foreach (var result in ScanResult.Results)
        {
            var confidence = ScanResult.CalculateConfidence(result.FillConfidence, result.DeltaConfidence).ToString("0.000", localeSpecifics.Culture);
            confidence += " " + IntToLetterAlphabetical(result.BestFilledAnswer);
            sb.Append(result.Points.ToString() ?? "!", confidence);
        }
        return sb;
    }

    public DualAutoSeparatedStringBuilder ToTaskCsv(int pageIndex, LocaleSpecifics localeSpecifics)
    {
        var sb = new DualAutoSeparatedStringBuilder(localeSpecifics.ListSeparator, (pageIndex + 1).ToString(), State.ToString());
        sb.Append(UserCode, "");
        sb.Append(TotalPoints.ToString(), "");
        sb.Append(TotalPoints >= SuccessfulMinimumScore ? localeSpecifics.SuccessText : localeSpecifics.FailText, "");
        var orderedResults = ScanResult.Results.OrderBy(r => r.TaskIndex.Get() ?? -1).ToArray();
        foreach (var result in orderedResults)
        {
            if (result.TaskIndex.Get() == null)
            {
                sb.Append("!", "");
                continue;
            }

            var confidence = ScanResult.CalculateConfidence(result.FillConfidence, result.DeltaConfidence).ToString("0.000", localeSpecifics.Culture);
            confidence += " " + IntToLetterAlphabetical(result.BestFilledAnswer);
            sb.Append(result.Points.ToString() ?? "!", confidence);
        }
        return sb;
    }

    /// Returns an integers value in the alphabet (0 => A, 2 => C, etc...).
    /// Values greater than 25 return successive UTF32 characters (26 => [).
    private string IntToLetterAlphabetical(int i) => Encoding.UTF32.GetString(BitConverter.GetBytes('A' + i));
}