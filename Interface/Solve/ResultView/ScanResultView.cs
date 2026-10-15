using System.ComponentModel;
using System.Globalization;
using System.Text;
using System.Windows;
using Scanner.Result;

namespace Interface.Solve.ResultView;

public struct LocaleSpecifics
{
    public CultureInfo Culture;
    public string ListSeparator;
    public string SuccessText;
    public string FailText;
}

internal class ScanResultView : INotifyPropertyChanged
{
    // TODO: This should also be refactored, the current look is just to quickly test if it works at all...
    
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
    private const int SuccessfulMinimumScore = Scanner.Result.ScanResult.SuccessScoreCount;
    private const string UnknownString = "?";

    public IScanResult ScanResult;

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
        get => ScanResultNullable?.UserCode.Get().Code.ToUpper() ?? string.Empty;
        set
        {
            if (Scanner.Codes.UserCode.TryCreate(value) is { } userCode)
            {
                ScanResultNullable?.UserCode.Set(userCode);
            }
            InvokePropertyChanged(nameof(UserCode));
        }
    }

    public string ExamCode => ScanResultNullable?.ExamCode.Get().Code ?? UnknownString;
    
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

    public Visibility IsResetVisible => (ScanResultNullable?.IsModified() ?? false) ? Visibility.Visible : Visibility.Collapsed;

    public ScanResultView(int page, IScanResult sourceResult)
    {
        ScanResult = sourceResult;
        Page = page;
        Points = new ScanResultViewPointAccess(sourceResult);
        Points.ElementChanged += (_, _) => InvokePropertyChanged(nameof(Points));
    }

    private ScanResultHuman GetScanResultHuman()
    {
        if (ScanResult is ScanResult scanResult)
            return scanResult.UserCode.Get().Code == string.Empty ? ScanResultHuman.Ok : ScanResultHuman.Partial;
        return ScanResultHuman.Error;
    }
    
    public DualAutoSeparatedStringBuilder ToSheetCsv(int pageIndex, LocaleSpecifics localeSpecifics)
    {
        var sb = ToCsvCommonHeader(pageIndex, localeSpecifics);
        foreach (var result in ScanResultNullable?.Results ?? [])
            ToCsvCommonResult(ref sb, result, localeSpecifics);
        return sb;
    }

    public DualAutoSeparatedStringBuilder ToTaskCsv(int pageIndex, LocaleSpecifics localeSpecifics)
    {
        var sb = ToCsvCommonHeader(pageIndex, localeSpecifics);
        var orderedResults = (ScanResultNullable?.Results ?? []).OrderBy(r => r.TaskIndex).ToArray();
        foreach (var result in orderedResults)
        {
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
        var confidence = result.Confidence.ToString("0.000", localeSpecifics.Culture);
        confidence += " " + IntToLetterAlphabetical(result.BestFilledAnswer);
        sb.Append(result.Points.ToString(), confidence);
    }

    public void Reset()
    {
        ScanResultNullable?.Reset();
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(UserCode)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Points)));
        InvokePropertyChangedDerived();
    }

    /// Returns an integers value in the alphabet (0 => A, 2 => C, etc...).
    /// Values greater than 25 return successive UTF32 characters (26 => '[').
    private static string IntToLetterAlphabetical(int i) => Encoding.UTF32.GetString(BitConverter.GetBytes('A' + i));

    public ScanResult? ScanResultNullable
    {
        get
        {
            if (ScanResult is ScanResult scanResult)
                return scanResult;
            return null;
        }
    }
}