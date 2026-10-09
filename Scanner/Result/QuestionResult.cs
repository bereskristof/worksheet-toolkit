using System.Diagnostics.CodeAnalysis;

namespace Scanner.Result;

public struct QuestionResult
{
    public const int CorrectAnswerPoints = 4;
    public const int EmptyAnswerPoints = 0;
    public const int WrongAnswerPoints = -1;

    public const double MinDeltaConfidence = 0.4; /// Minimum difference between best and next best answer to consider it not double filled
    public const double MinFillConfidence = 0.1; /// Minimum % of pixels filled in the bubble to consider it filled (0.2 skips)
    
    public required Rollbackable<int?> TaskIndex;
    public required Rollbackable<int?> Points;
    public int BestFilledAnswer;
    public double DeltaConfidence;
    public double FillConfidence;
}