using Scanner.Result;

namespace Interface.Solve;

public record ScanResultViewPointAccess
{
    private readonly ScanResult _scanResult;
    
    internal ScanResultViewPointAccess(ScanResult scanResult)
    {
        _scanResult = scanResult;
    }

    public int? this[int index]
        => (_scanResult.Results.Length > index) ? (_scanResult.Results[index].Points ?? 0) : null;
    
    internal int[] Get() => [.. _scanResult.Results.Select(r => r.Points ?? 0)];
}