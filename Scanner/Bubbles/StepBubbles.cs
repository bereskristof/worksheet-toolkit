using System.Drawing;
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
    private readonly Bitmap _bitmap;

    internal StepCodesBubbles(CodePack codes, byte[] image, Bitmap bitmap)
    {
        _image = image;
        _codes = codes;
        _bitmap = bitmap;
    }

    public IStepCheck GetSelectedAnswers()
    {
        try
        {
            var questionCount = GetQuestionCount();
            var answerBubbles = GetAnswerBubbles(questionCount);
            var userMarkings = GetUserMarkings(answerBubbles, questionCount);
            var filledAnswers = GetFilledAnswers(userMarkings);
            return new StepCheck(_codes, filledAnswers, _bitmap);
        }
        catch (Exception e)
        {
            return new StepCodesError(e);
        }
    }

    private uint GetQuestionCount()
    {
        return ExamResultObtainer.ObtainExamQuestionCount(_codes.ExamCode.Code);
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