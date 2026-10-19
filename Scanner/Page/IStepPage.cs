using Docnet.Core.Readers;
using Scanner.Codes;

namespace Scanner.Page;

public interface IStepPage
{
    /// Obtain the provided page as a bitmap.
    public IStepCodes GetPage(IDocReader reader, int pageNumber);
}