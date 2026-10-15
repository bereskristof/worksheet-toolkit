using Docnet.Core.Readers;
using Scanner.Bubbles;
using Scanner.Check;
using Scanner.Codes;
using Scanner.Collect;
using Scanner.Page;
using Scanner.Result;

namespace Scanner.Error;

/// Opaque error states until the pipeline is finished.
internal class StepCodesError : IStepPage, IStepCodes, IStepCodesBubbles, IStepCheck, IStepCollect
{
    private readonly Exception _error;

    internal StepCodesError(Exception error)
    {
        _error = error;
    }
    
    public IStepCodes GetPage(IDocReader reader, int pageNumber) => this;
    
    public IStepCodesReturnable GetCodes() => this;
    public IStepCodesBubbles GetCodesDataEntry(DataEntry dataEntry) => this;
    public IStepCodesBubbles GetCodesSkipDataEntry(MissingDataEntry missingDataEntry) => this;
    
    public IStepCheck GetSelectedAnswers() => this;

    public IStepCollect CheckAnswers() => this;
    
    public IScanResult CollectData()
    {
        return new ScanError(_error);
    }
}