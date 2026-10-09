using System.Globalization;

namespace Scanner.Result;

public struct LocaleSpecifics
{
    public CultureInfo Culture;
    public string DecimalPoint;
    public string ListSeparator;
    public string SuccessText;
    public string FailText;
}

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
    
    public Guid? ExamCode;
    public string? UserCode;
    
    public int? FinalPoints;
    
    public QuestionResult[] Results;
    
    public DualAutoSeparatedStringBuilder ToSheetCsv(int pageIndex, LocaleSpecifics localeSpecifics)
    {
        var sb = new DualAutoSeparatedStringBuilder(localeSpecifics.ListSeparator, (pageIndex + 1).ToString(), CurrentState.ToString());
        sb.Append(UserCode ?? "???", ""); // ??? is for possible errors, !!! is for development errors
        if (ExamCode == null)
        {
            return sb;
        }
        sb.Append(FinalPoints.ToString() ?? "!!!", "");
        if (FinalPoints == null)
        {
            return sb;
        }
        sb.Append(FinalPoints >= SuccessScoreCount ? localeSpecifics.SuccessText : localeSpecifics.FailText, "");
        foreach (var result in Results)
        {
            string[] choices = ["A", "B", "C", "D", "E"];
            var confidence = CalculateConfidence(result.FillConfidence, result.DeltaConfidence).ToString("0.000", localeSpecifics.Culture);
            confidence += " " + choices[result.BestFilledAnswer] + ":" + choices[result.BestFilledAnswer];
            sb.Append(result.Points.ToString() ?? "!!!", confidence);
        }
        return sb;
    }

    public DualAutoSeparatedStringBuilder ToTaskCsv(int pageIndex, LocaleSpecifics localeSpecifics)
    {
        var sb = new DualAutoSeparatedStringBuilder(localeSpecifics.ListSeparator, (pageIndex + 1).ToString(), CurrentState.ToString());
        sb.Append(UserCode ?? "???", ""); // ??? is for possible errors, !!! is for development errors
        if (ExamCode == null)
        {
            return sb;
        }
        sb.Append(FinalPoints.ToString() ?? "!!!", "");
        if (FinalPoints == null)
        {
            return sb;
        }
        sb.Append(FinalPoints >= SuccessScoreCount ? localeSpecifics.SuccessText : localeSpecifics.FailText, "");
        var orderedResults = Results.OrderBy(r => r.TaskIndex ?? -1).ToArray();
        foreach (var result in orderedResults)
        {
            if (result.TaskIndex == null)
            {
                sb.Append("!!!", "");
                continue;
            }

            string[] choices = ["A", "B", "C", "D", "E"];
            var confidence = CalculateConfidence(result.FillConfidence, result.DeltaConfidence).ToString("0.000", localeSpecifics.Culture);
            confidence += " " + choices[result.BestFilledAnswer];
            sb.Append(result.Points.ToString() ?? "!!!", confidence);
        }
        return sb;
    }
    
    private static double CalculateConfidence(double bestFillPercent, double nextBestRatio)
    {
        const double v = 15;
        var fillConfidence = double.Min(1, double.Abs(v * bestFillPercent - v * QuestionResult.MinFillConfidence));
        var ratioConfidence = double.Min(1, double.Abs(v * nextBestRatio - v * QuestionResult.MinDeltaConfidence));
        var totalConfidence = ratioConfidence * fillConfidence;
        return totalConfidence;
    }
}