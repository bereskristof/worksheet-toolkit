using Scanner.Result;

namespace Scanner.Collect;

public union ScanResults(ScanError, ScanResult);

public interface IStepCollect
{
    public ScanResults CollectData();
}