using Scanner.Managing;
using Scanner.Result;

namespace Scanner.Collect;

public union StepCollectReturnables(ScanResults, MissingData);

public union AnyData(FilledData, MissingData);

public union ScanResults(ScanError, ScanResult);

public interface IStepCollect
{
    public StepCollectReturnables CollectData();
    
    public ScanResults CollectData(AnyData anyData);
}