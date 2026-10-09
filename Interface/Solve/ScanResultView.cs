using System.ComponentModel;
using System.Globalization;
using System.Text;
using System.Windows;
using Scanner;
using Scanner.Result;

namespace Interface.Solve;

public struct LocaleSpecifics
{
    public CultureInfo Culture;
    public string ListSeparator;
    public string SuccessText;
    public string FailText;
}

internal class ScanResultView : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private void InvokePropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        InvokePropertyChangedDerived();
    }

    private void InvokePropertyChangedDerived()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TotalPoints)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Result)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(StateString)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MinorError)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MajorError)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsResetVisible)));
    }

    private enum ScanResultHuman
    {
        Ok,
        Partial,
        Error,
    }
    
    /// Score to have a successful test.
    private const int SuccessfulMinimumScore = ScanResult.SuccessScoreCount;
    private const string UnknownString = "?";

    public ScanResult ScanResult;

    public int Page { get; init; }

    public string StateString => GetScanResultHuman() switch
    {
        ScanResultHuman.Ok => Resources.Lang.Solve_StateOk,
        ScanResultHuman.Partial => Resources.Lang.Solve_StateWarning,
        _ => Resources.Lang.Solve_StateError,
    };
    public bool MajorError => GetScanResultHuman() == ScanResultHuman.Error;
    public bool MinorError => GetScanResultHuman() == ScanResultHuman.Partial;
    
    public string UserCode
    {
        get => (ScanResult.UserCode.Get() ?? UnknownString).ToUpper();
        set
        {
            if (CodeScanner.IsNeptunCode(value))
            {
                ScanResult.UserCode.Set(value);
            }
            InvokePropertyChanged(nameof(UserCode));
        }
    }

    public string ExamCode => ScanResult.ExamCode.Get()?.ToString() ?? UnknownString;
    
    public int? TotalPoints => (GetScanResultHuman() != ScanResultHuman.Error) ? Points.Get().Sum() : null;
    public int TaskCount => Points.Get().Length;
    public string? Result
    {
        get
        {
            if (TotalPoints is { } totalPoints)
            {
                return (totalPoints >= SuccessfulMinimumScore)
                    ? Resources.Lang.Export_ResultSuccess
                    : Resources.Lang.Export_ResultFailure;
            }
            return null;
        }
    }

    public ScanResultViewPointAccess Points { get; init; }

    public Visibility IsResetVisible => ScanResult.IsModified() ? Visibility.Visible : Visibility.Collapsed;

    public ScanResultView(int page, ScanResult sourceResult)
    {
        ScanResult = sourceResult;
        Page = page;
        Points = new ScanResultViewPointAccess(sourceResult);
        Points.ElementChanged += (_, _) => InvokePropertyChanged(nameof(Points));
    }

    private ScanResultHuman GetScanResultHuman()
    {
        if (ScanResult.Issues.Get() != ScanResult.IssueFlags.None ||
            ScanResult.GetMissingFields().HasFlag(ScanResult.MissingFieldFlags.MissingExamCode))
            return ScanResultHuman.Error;
        if (ScanResult.GetMissingFields().HasFlag(ScanResult.MissingFieldFlags.MissingUserCode))
            return ScanResultHuman.Partial;
        return ScanResultHuman.Ok;
    }
    
    public DualAutoSeparatedStringBuilder ToSheetCsv(int pageIndex, LocaleSpecifics localeSpecifics)
    {
        var sb = ToCsvCommonHeader(pageIndex, localeSpecifics);
        foreach (var result in ScanResult.Results)
            ToCsvCommonResult(ref sb, result, localeSpecifics);
        return sb;
    }

    public DualAutoSeparatedStringBuilder ToTaskCsv(int pageIndex, LocaleSpecifics localeSpecifics)
    {
        var sb = ToCsvCommonHeader(pageIndex, localeSpecifics);
        var orderedResults = ScanResult.Results.OrderBy(r => r.TaskIndex.Get() ?? -1).ToArray();
        foreach (var result in orderedResults)
        {
            if (result.TaskIndex.Get() == null)
            {
                sb.Append("!", "");
                continue;
            }
            ToCsvCommonResult(ref sb, result, localeSpecifics);
        }
        return sb;
    }

    public DualAutoSeparatedStringBuilder ToCsvCommonHeader(int pageIndex, LocaleSpecifics localeSpecifics)
    {
        var sb = new DualAutoSeparatedStringBuilder(localeSpecifics.ListSeparator, (pageIndex + 1).ToString(),
            "");
        sb.Append(UserCode, "");
        sb.Append(TotalPoints.ToString() ?? "", "");
        sb.Append(TotalPoints >= SuccessfulMinimumScore ? localeSpecifics.SuccessText : localeSpecifics.FailText, "");
        return sb;
    }

    public static void ToCsvCommonResult(ref DualAutoSeparatedStringBuilder sb, QuestionResult result,
        LocaleSpecifics localeSpecifics)
    {
        var confidence = ScanResult.CalculateConfidence(result.FillConfidence, result.DeltaConfidence)
            .ToString("0.000", localeSpecifics.Culture);
        confidence += " " + IntToLetterAlphabetical(result.BestFilledAnswer);
        sb.Append(result.Points.ToString(), confidence);
    }

    public void Reset()
    {
        ScanResult.Reset();
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(UserCode)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Points)));
        InvokePropertyChangedDerived();
    }

    /// Returns an integers value in the alphabet (0 => A, 2 => C, etc...).
    /// Values greater than 25 return successive UTF32 characters (26 => '[').
    private static string IntToLetterAlphabetical(int i) => Encoding.UTF32.GetString(BitConverter.GetBytes('A' + i));
}