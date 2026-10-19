using Scanner.Collect;
using Scanner.Result;

namespace Interface.Solve.ResultView;

public record ScanResultViewPointAccess
{
    public event EventHandler? ElementChanged;
    
    private readonly ScanResults _scanResult;
    
    internal ScanResultViewPointAccess(ScanResults scanResult)
    {
        _scanResult = scanResult;
    }

    public int? this[int index]
    {
        get {
            if (_scanResult is not ScanResult r) return null;
            return (r.Results.Length > index) ? (r.Results [index].Points.Get()) : null;
        }
        set
        {
            if (_scanResult is not ScanResult r) return;
            if (r.Results.Length <= index) return;
            if (value is not { } points) return;
            r.Results[index].Points.Set(points);
            ElementChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    internal int[] Get()
    {
        if (_scanResult is not ScanResult res) return [];
        return [.. res.Results.Select(r => r.Points.Get())];
    }
}