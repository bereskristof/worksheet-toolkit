using Microsoft.Data.Sqlite;

namespace Storage;

public static class Manager
{
    public const string PathTitle = "WorksheetToolkit";
    
    private static SqliteConnection? _connection;

    public static SqliteConnection Connection
    {
        get
        {
            if (_connection != null) return _connection;
            Log.Write("Null reference in Manager.Connection.get", Log.Severity.Error);
            throw new NullReferenceException("Access is invalid, database connection is null");
        }
        private set => _connection = value;
    }
    
    private static CodeGenerator? _codeGenerator;

    public static CodeGenerator CodeGenerator
    {
        get
        {
            if (_connection != null && _codeGenerator != null) return _codeGenerator;
            Log.Write("Null reference in Manager.CodeGenerator.get", Log.Severity.Error);
            throw new NullReferenceException("Access is invalid, database or code generator connection is null");
        }
        private set => _codeGenerator = value;
    }
    
    public static void CreateDatabase(string filename, string password)
    {
        var connectionString = $"Data Source={filename}";
        Connection = new SqliteConnection(connectionString);
        Connection.Open();
        
        var cleanCommand = Connection.CreateCommand();
        cleanCommand.CommandText = "DROP TABLE IF EXISTS Questions; " +
                                   "DROP TABLE IF EXISTS Answers; " +
                                   "DROP TABLE IF EXISTS Images; " +
                                   "DROP TABLE IF EXISTS Security;";
        cleanCommand.ExecuteNonQuery();
        cleanCommand.Dispose();
        
        Tables.Init(Connection);
        IdManager.Init();
        
        Encryption.SavePassword(password);
        Encryption.TryPassword(password);
        
        CodeGenerator = new CodeGenerator(Connection);
        
        Log.Write("Created database");
    }

    /// Returns true if the database was opened successfully, false otherwise.
    public static bool OpenDatabase(string filename)
    {
        var connectionString = $"Data Source={filename}; Mode=ReadWrite";
        Connection = new SqliteConnection(connectionString);
        try
        {
            Connection.Open();
        }
        catch (SqliteException)
        {
            return false;
        }

        Tables.Init(Connection);
        IdManager.Init();
        CodeGenerator = new CodeGenerator(Connection);
        
        Log.Write("Opened database");
        return true;
    }

    public static void CloseDatabase()
    {
        Connection.Close();
        Log.Write("Closed database");
    }
}
