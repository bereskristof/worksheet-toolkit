namespace Scanner.Bubbles;

public struct SelectedAnswer
{
    internal readonly int BestFilledAnswer;
    internal readonly double DeltaConfidence;
    internal readonly double FillConfidence;

    public SelectedAnswer(BubbleScanResult userMarking)
    {
        var (mResult, mBest, mNextBest) = userMarking;
        BestFilledAnswer = mResult;
        // [0, 1], since mNextBest <= mBest
        DeltaConfidence = double.IsNaN((mBest - mNextBest) / mBest) ? 1.0 : ((mBest - mNextBest) / mBest);
        // [0, 1], since it's a ratio
        FillConfidence = mBest;
    }
}