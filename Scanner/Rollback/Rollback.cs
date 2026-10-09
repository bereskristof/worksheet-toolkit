using System.Diagnostics.Contracts;

namespace Scanner.Rollback;

/// Create a T value that can be user changed,
/// with a guarantee to always allow rolling back to it's initial value.
public struct Rollback<T>(T initial) : IRollback
{
    private readonly T _initial = initial;
    private T _override = initial;
    
    [Pure]
    public T Get() => _override;
    
    public void Set(T @override) => _override = @override;
    
    public void Reset() => _override = _initial;
    
    public bool IsModified() => Comparer<T>.Default.Compare(_initial, _override) != 0;

    public override string ToString() => _override?.ToString() ?? string.Empty;
}