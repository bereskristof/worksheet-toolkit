using System.ComponentModel;
using System.Globalization;
using System.Text;
using System.Windows;
using Scanner.Collect;
using Scanner.Managing;
using Scanner.Result;

namespace Interface.Solve.ResultView;

public struct LocaleSpecifics
{
    public CultureInfo Culture;
    public string ListSeparator;
    public string SuccessText;
    public string FailText;
}

internal class ScanResultView : INotifyPropertyChanged, IComparable<ScanResultView>
{
    // https://learn.microsoft.com/en-us/windows/apps/design/iconography/segoe-ui-symbol-font#pua-e700-e900
    private const string ExtraResetLabel = "\xE845"; // RevToggleKey
    private const string ExtraIssueLabel = "\xE897"; // Help
    
    public event PropertyChangedEventHandler? PropertyChanged;

    private enum ScanResultHuman
    {
        Ok,
        Partial,
        Error,
    }
    
    /// Score to have a successful test.
    private const int SuccessfulMinimumScore = ScanResult.SuccessScoreCount;
    private const string UnknownString = "?";

    public Result Backing;

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
        get => Backing.Ok?.UserCode.Get().Code.ToUpper() ?? Backing.Err?.UserCode.Code.ToUpper() ?? string.Empty;
        set => Backing.OnOk(result =>
            {
                if (Scanner.Managing.UserCode.Create(value).TryValidate() is { } userCode)
                    result.UserCode.Set(userCode);
                InvokePropertyChanged(nameof(UserCode));
            });
    }

    public string ExamCode => Backing.Ok?.UserCode.Get().Code ?? UnknownString;
    
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

    public Visibility IsExtraVisible => (Backing.Ok?.IsModified() ?? Backing.Err is not null) ? Visibility.Visible : Visibility.Collapsed;
    public string ExtraTitle => (Backing.Err is null) ? ExtraResetLabel : ExtraIssueLabel;

    public ScanResultView(int page, ScanResults sourceResult)
    {
        Backing = new Result(sourceResult);
        Page = page;
        Points = new ScanResultViewPointAccess(sourceResult);
        Points.ElementChanged += (_, _) => InvokePropertyChanged(nameof(Points));
    }

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
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsExtraVisible)));
    }

    private ScanResultHuman GetScanResultHuman()
    {
        if (Backing.Ok is { } scanResult)
            return string.IsNullOrEmpty(scanResult.UserCode.Get().Code) ? ScanResultHuman.Partial : ScanResultHuman.Ok;
        return ScanResultHuman.Error;
    }
    
    public DualAutoSeparatedStringBuilder ToSheetCsv(int pageIndex, LocaleSpecifics localeSpecifics)
    {
        var sb = ToCsvCommonHeader(pageIndex, localeSpecifics);
        foreach (var result in Backing.Ok?.Results ?? [])
            ToCsvCommonResult(ref sb, result, localeSpecifics);
        return sb;
    }

    public DualAutoSeparatedStringBuilder ToTaskCsv(int pageIndex, LocaleSpecifics localeSpecifics)
    {
        var sb = ToCsvCommonHeader(pageIndex, localeSpecifics);
        var orderedResults = (Backing.Ok?.Results ?? []).OrderBy(r => r.TaskIndex).ToArray();
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
        var pointsString = TotalPoints switch
        {
            { } points => (points >= SuccessfulMinimumScore ? localeSpecifics.SuccessText : localeSpecifics.FailText),
            null => "",
        };
        sb.Append(pointsString, "");
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
        Backing.OnOk(result => result.Reset());
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(UserCode)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Points)));
        InvokePropertyChangedDerived();
    }

    public string GetError()
    {
        if (Backing.Err is not { } error)
            return string.Empty;
        return error.Error.Message;
    }

    /// Returns an integers value in the alphabet (0 => A, 2 => C, etc...).
    /// Values greater than 25 return successive UTF32 characters (26 => '[').
    private static string IntToLetterAlphabetical(int i) => Encoding.UTF32.GetString(BitConverter.GetBytes('A' + i));

    public int CompareTo(ScanResultView? other) => Page.CompareTo(other?.Page);
}