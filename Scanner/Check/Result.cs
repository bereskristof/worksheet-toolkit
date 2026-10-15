namespace Scanner.Check;

public readonly struct Result
{
    public enum AnswerResult
    {
        IncorrectAnswer = -1,
        NoAnswerMarked = 0,
        CorrectAnswer = 4,
    }

    public readonly AnswerResult Points;
    public readonly double Confidence;

    internal Result(AnswerResult answerResult, double bestFillPercent, double nextBestRatio)
    {
        Points = answerResult;
        Confidence = CalculateConfidence(bestFillPercent, nextBestRatio);
    }
    
    private static double CalculateConfidence(double bestFillPercent, double nextBestRatio)
    {
        const double v = 15;
        var fillConfidence = double.Min(1, double.Abs(v * bestFillPercent - v * StepCheck.MinFillConfidence));
        var ratioConfidence = double.Min(1, double.Abs(v * nextBestRatio - v * StepCheck.MinDeltaConfidence));
        var totalConfidence = ratioConfidence * fillConfidence;
        return totalConfidence;
    }
}