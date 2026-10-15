using Scanner.Rollback;

namespace Scanner.Result;

public struct QuestionResult
{
    public Rollback<int> Points;
    public readonly int TaskIndex;
    public readonly int BestFilledAnswer;
    public readonly double DeltaConfidence;
    public readonly double FillConfidence;
    public readonly double Confidence;
    
    internal QuestionResult(int points, int taskIndex, int bestFilledAnswer, double deltaConfidence, double fillConfidence, double confidence)
    {
        Points = new Rollback<int>(points);
        TaskIndex = taskIndex;
        BestFilledAnswer = bestFilledAnswer;
        DeltaConfidence = deltaConfidence;
        FillConfidence = fillConfidence;
        Confidence = confidence;
    }
}