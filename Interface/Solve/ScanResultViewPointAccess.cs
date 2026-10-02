using Scanner.Result;

namespace Interface.Solve;

public record ScanResultViewPointAccess
{
    public event EventHandler? ElementChanged;
    
    private readonly ScanResult _scanResult;
    
    internal ScanResultViewPointAccess(ScanResult scanResult)
    {
        _scanResult = scanResult;
    }

    public int? this[int index]
    {
        get => (_scanResult.Results.Length > index) ? (_scanResult.Results[index].Points.Get() ?? 0) : null;
        set
        {
            if (_scanResult.Results.Length <= index) return;
            _scanResult.Results[index].Points.Set(value);
            ElementChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    internal int[] Get() => [.. _scanResult.Results.Select(r => r.Points.Get() ?? 0)];
}