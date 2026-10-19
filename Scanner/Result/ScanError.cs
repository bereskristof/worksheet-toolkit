using Scanner.Managing;

namespace Scanner.Result;

/// A possible result of the scanning process.
/// Means some error occured, and the results could not be obtained.
public class ScanError
{
    public readonly Exception Error;
    public readonly UserCode UserCode;
    
    internal ScanError(Exception error, UserCode userCode)
    {
        Error = error;
        UserCode = userCode;
    }
}