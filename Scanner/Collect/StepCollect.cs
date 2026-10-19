using System.Diagnostics;
using System.Drawing;
using Scanner.Bubbles;
using Scanner.Codes;
using Scanner.Managing;
using Scanner.Result;
using Scanner.Rollback;

namespace Scanner.Collect;

public class StepCollect : IStepCollect
{
    private readonly Bitmap _bitmap;
    private Guid _dataEntryVerify = Guid.Empty;
    
    private readonly CodePack _codes;
    private readonly SelectedAnswer[] _selectedAnswers;
    private readonly Check.Result[] _results;

    private ScanResult? _missingData;

    internal StepCollect(CodePack codes, SelectedAnswer[] selectedAnswers, Check.Result[] results, Bitmap bitmap)
    {
        _codes = codes;
        _selectedAnswers = selectedAnswers;
        _results = results;
        _bitmap = bitmap;
    }

    public StepCollectReturnables CollectData()
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
        examCode.Set(_codes.ExamCode);
        var userCode = new Rollback<UserCode>(_codes.InitialUserCode);
        userCode.Set(_codes.UserCode);
        
        _missingData = new ScanResult(examCode, userCode, questionResults);
        if (_codes.UserCode.TryValidate() is null)
        {
            return MissingData.Init(_codes.ExamCode, _codes.UserCode, _bitmap, true, out _dataEntryVerify);
        }
        
        return new ScanResults(new ScanResult(examCode, userCode, questionResults));
    }

    public ScanResults CollectData(AnyData anyData)
    {
        // TODO: For now, the exam code field is locked if the sheet is already checked.
        // Maybe this function should instead return an IStepBubbles if the exam code was changed?
        var userCode = new Rollback<UserCode>(_codes.InitialUserCode);
        switch (anyData)
        {
            case MissingData missing:
                if (missing.OwnerKey != _dataEntryVerify)
                    throw new InvalidDataException("CollectData: The provided MissingDataEntry does not match the entry that was given!");
                userCode.Set(missing.UserCode);
                break;
            case FilledData filled:
                if (filled.OwnerKey != _dataEntryVerify)
                    throw new InvalidDataException("CollectData: The provided MissingDataEntry does not match the entry that was given!");
                userCode.Set(filled.UserCode);
                break;
        }

        _missingData?.UserCode = userCode;
        return _missingData ?? throw new UnreachableException();
    }
}