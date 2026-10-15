using OpenCvSharp;
using Scanner.Check;
using Scanner.Codes;
using Scanner.Error;
using Storage;

namespace Scanner.Bubbles;

public class StepCodesBubbles : IStepCodesBubbles
{
    private const uint AnswerCount = 5;
    
    private readonly byte[] _image;
    private readonly CodePack _codes;

    internal StepCodesBubbles(CodePack codes, byte[] image)
    {
        _image = image;
        _codes = codes;
    }

    public IStepCheck GetSelectedAnswers()
    {
        try
        {
            var questionCount = GetQuestionCount();
            var answerBubbles = GetAnswerBubbles(questionCount);
            var userMarkings = GetUserMarkings(answerBubbles, questionCount);
            var filledAnswers = GetFilledAnswers(userMarkings);
            return new StepCheck(_codes, filledAnswers);
        }
        catch (Exception e)
        {
            return new StepCodesError(e);
        }
    }

    private uint GetQuestionCount()
    {
        return ExamResultObtainer.ObtainExamQuestionCount(_codes.ExamCode.Inner.Code);
    }

    private CircleSegment[] GetAnswerBubbles(uint questionCount)
    {
        var matrixScanner = new MatrixScanner(_image);
        return matrixScanner.FindBubbles(questionCount, AnswerCount);
    }

    private BubbleScanResult[] GetUserMarkings(CircleSegment[] answerBubbles, uint questionCount)
    {
        var bubbleChecker = new BubbleChecker(_image);
        return bubbleChecker.CheckBubbles(answerBubbles, questionCount, AnswerCount);
    }
    
    private static SelectedAnswer[] GetFilledAnswers(BubbleScanResult[] userMarkings)
    {
        return [.. userMarkings.Select(mark => new SelectedAnswer(mark))];
    }
}