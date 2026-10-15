using Scanner.Bubbles;
using Scanner.Codes;
using Scanner.Collect;
using Scanner.Error;
using Storage;

namespace Scanner.Check;

public class StepCheck : IStepCheck
{
    internal const double MinDeltaConfidence = 0.4; /// Minimum difference between best and next best answer to consider it not double filled
    internal const double MinFillConfidence = 0.1; /// Minimum % of pixels filled in the bubble to consider it filled (0.2 skips)
    
    private readonly CodePack _codes;
    private readonly SelectedAnswer[] _selectedAnswers;

    internal StepCheck(CodePack codePack, SelectedAnswer[] selectedAnswers)
    {
        _codes = codePack;
        _selectedAnswers = selectedAnswers;
    }

    public IStepCollect CheckAnswers()
    {
        try
        {
            var correctAnswers = ExamResultObtainer.ObtainCorrectAnswers(_codes.ExamCode.Inner.Code);
            var results = CorrectAnswers(correctAnswers);
            return new StepCollect(_codes, _selectedAnswers, results);
        }
        catch (Exception e)
        {
            return new StepCodesError(e);
        }
    }

    private Result[] CorrectAnswers(CorrectAnswer[] correctAnswers)
    {
        if (_selectedAnswers.Length != correctAnswers.Length)
            throw new InvalidDataException("CorrectAnswers: Selected answers and correct answers are not the same length.");
        var length = _selectedAnswers.Length;
        var results = new Result[length];
        
        for (var i = 0; i < length; i++)
        {
            var result = Result.AnswerResult.NoAnswerMarked;
            
            var selectedAnswer = _selectedAnswers[i];
            var correctAnswer = correctAnswers[i];
            
            if (selectedAnswer.FillConfidence < MinFillConfidence)
            {
                result = Result.AnswerResult.NoAnswerMarked;
            }
            else if (selectedAnswer.DeltaConfidence <= MinDeltaConfidence)
            {
                result = Result.AnswerResult.IncorrectAnswer; // Multiple answers marked
            }

            result = (selectedAnswer.BestFilledAnswer == correctAnswer.AnswerNumber) switch
            {
                true => Result.AnswerResult.CorrectAnswer,
                false => Result.AnswerResult.IncorrectAnswer,
            };
            
            results[i] = new Result(result, selectedAnswer.FillConfidence, selectedAnswer.DeltaConfidence);
        }
        
        return results;
    }
}