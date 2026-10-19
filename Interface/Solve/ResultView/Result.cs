using Scanner.Collect;
using Scanner.Result;

namespace Interface.Solve.ResultView;

public class Result(ScanResults backing)
{
    public ScanResult? Ok => backing switch
    {
        ScanResult result => result,
        _ => null
    };

    public ScanError? Err => backing switch
    {
        ScanError error => error,
        _ => null
    };

    public void OnOk(Action<ScanResult> onOk)
    {
        if (backing is ScanResult ok)
            onOk(ok);
    }
}