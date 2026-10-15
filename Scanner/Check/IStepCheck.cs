using Scanner.Collect;

namespace Scanner.Check;

public interface IStepCheck
{
    /// Attempts to get which bubbles have been filled, and return points based on that.
    public IStepCollect CheckAnswers();
}