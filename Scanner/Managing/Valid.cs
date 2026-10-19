namespace Scanner.Managing;

/// Lightweight contained with the added guarantee of the data not being empty.
/// This is required to make rolling back to no exam code possible while not allowing
/// to pass nullable exam codes to steps that require an exam code.
public struct Valid<T> where T : ICode<T>
{
    private readonly T _inner;

    public string Code => _inner.Code;

    private Valid(T code)
    {
        _inner = code;
    }

    /// Verify that the provided ExamCode isn't null.
    internal static Valid<T>? TryValidate(T code)
    {
        if (string.IsNullOrEmpty(code.Code))
            return null;
        return new Valid<T>(code);
    }
    
    public static implicit operator T(Valid<T> code) => code._inner;
}