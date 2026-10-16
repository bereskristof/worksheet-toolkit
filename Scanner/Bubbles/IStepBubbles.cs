using Scanner.Check;
using Scanner.Codes;

namespace Scanner.Bubbles;

public interface IStepCodesBubbles
{
    /// Attempts to get which bubbles have been filled.
    public IStepCheck GetSelectedAnswers();
}