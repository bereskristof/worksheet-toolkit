using Scanner.Bubbles;

namespace Scanner.Codes;

public union StepCodesReturnable(IStepCodesBubbles, MissingDataEntry);

public interface IStepCodes
{
    /// Attempts to get the pages task & user code.
    /// The return value must be pattern matched.
    public StepCodesReturnable GetCodes();
    
    /// Assuming a MissingDataEntry was returned from Step(),
    /// use this method to return the fixed instance of it.
    public IStepCodesBubbles GetCodesDataEntry(DataEntry dataEntry);

    /// Assuming a MissingDataEntry was returned from Step(),
    /// use this method to skip returning valid data.
    /// If only the user code is missing, a solve may still be attempted.
    public IStepCodesBubbles GetCodesSkipDataEntry(MissingDataEntry missingDataEntry);
}