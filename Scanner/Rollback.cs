using System.Diagnostics.Contracts;

namespace Scanner;

/// Create a T value that can be user changed,
/// with a guarantee to always allow rolling back to it's initial value.
public struct Rollback<T>(T initial)
{
    // NOTE: struct here forces sane nullability!
    
    private readonly T _initial = initial;
    private T _override = initial;
    
    [Pure]
    public T Get() => _override;
    
    public void Set(T @override) => _override = @override;
    
    public void Reset() => _override = _initial;

    public override string ToString() => _override?.ToString() ?? string.Empty;
}