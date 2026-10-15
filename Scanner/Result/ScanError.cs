namespace Scanner.Result;

/// A possible result of the scanning process.
/// Means some error occured, and the results could not be obtained.
public class ScanError : IScanResult
{
    public readonly Exception Error;
    
    internal ScanError(Exception error)
    {
        Error = error;
    }
}