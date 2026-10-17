using Docnet.Core.Readers;
using Scanner.Bubbles;
using Scanner.Check;
using Scanner.Codes;
using Scanner.Collect;
using Scanner.Managing;
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
    
    public StepCodesReturnable GetCodes() => this;
    public IStepCodesBubbles GetCodes(FilledData _) => this;
    public IStepCodesBubbles GetCodes(MissingData _) => this;
    
    public IStepCheck GetSelectedAnswers() => this;

    public IStepCollect CheckAnswers() => this;
    
    public StepCollectReturnables CollectData()
    {
        return new ScanResults(new ScanError(_error));
    }

    public ScanResults CollectData(AnyData _)
    {
        return new ScanError(_error);
    }
}