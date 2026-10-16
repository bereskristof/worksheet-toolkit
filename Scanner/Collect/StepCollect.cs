using System.Diagnostics;
using Scanner.Bubbles;
using Scanner.Codes;
using Scanner.Result;
using Scanner.Rollback;

namespace Scanner.Collect;

public class StepCollect : IStepCollect
{
    private readonly CodePack _codes;
    private readonly SelectedAnswer[] _selectedAnswers;
    private readonly Check.Result[] _results;

    internal StepCollect(CodePack codes, SelectedAnswer[] selectedAnswers, Check.Result[] results)
    {
        _codes = codes;
        _selectedAnswers = selectedAnswers;
        _results = results;
    }

    public ScanResults CollectData()
    {
        if (_selectedAnswers.Length != _results.Length)
            // By necessity not possible
            throw new UnreachableException("StepCollectData: Selected answers and correct answers are not the same length.");
        
        var questionResults = _selectedAnswers
            .Zip(_results)
            .Select((zip, i) => {
                var (s, r) = zip;
                return new QuestionResult(
                    (int)r.Points,
                    i,
                    s.BestFilledAnswer,
                    s.DeltaConfidence,
                    s.FillConfidence,
                    r.Confidence
                );
            }).ToArray();
        var examCode = new Rollback<ExamCode>(_codes.InitialExamCode);
        examCode.Set(_codes.ExamCode.Inner);
        var userCode = new Rollback<UserCode>(_codes.InitialUserCode);
        userCode.Set(_codes.UserCode);
        
        return new ScanResult(examCode, userCode, questionResults);
    }
}