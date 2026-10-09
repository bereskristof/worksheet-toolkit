namespace Scanner.Rollback;

// Used to make non-generic methods not require concrete classes.
public interface IRollback
{
    public void Reset();
    
    public bool IsModified();
}