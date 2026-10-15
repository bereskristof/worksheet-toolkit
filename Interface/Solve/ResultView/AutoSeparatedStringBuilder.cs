using System.Text;

namespace Interface.Solve.ResultView;

public class DualAutoSeparatedStringBuilder
{
    private readonly StringBuilder _builder = new StringBuilder();
    private readonly StringBuilder _extraBuilder = new StringBuilder();
    private readonly string _separator;

    public DualAutoSeparatedStringBuilder(string separator, string initialText, string extraInitialText)
    {
        _separator = separator;
        _builder.Append(initialText);
        _extraBuilder.Append(extraInitialText);
    }
    
    public DualAutoSeparatedStringBuilder Append(string value, string extraValue)
    {
        _builder.Append(_separator);
        _builder.Append(value);
        _extraBuilder.Append(_separator);
        _extraBuilder.Append(extraValue);
        return this;
    }
    
    public override string ToString()
    {
        return _builder + "\n";
    }
    
    public string ToExtraString()
    {
        return _extraBuilder + "\n";
    }
}