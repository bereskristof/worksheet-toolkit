using System.Collections;
using System.Runtime.InteropServices.Swift;
using Microsoft.Data.Sqlite;
using Storage.Sheet;
using static System.Math;

namespace Storage;

public class MissingTaskFromDatabase(long missingTask) : Exception
{
    public override string ToString()
    {
        return $"Task number {missingTask} was not found in the database.";
    }
}

public readonly struct CorrectAnswer
{
    public readonly long AnswerNumber;
    public readonly long QuestionId;
    
    internal CorrectAnswer(long answerNumber, long questionId)
    {
        AnswerNumber = answerNumber;
        QuestionId = questionId;
    }
}

public class ExamKeysIterator : IEnumerator<string>, IEnumerable<string>
{
    private readonly SqliteDataReader _inner;
    
    string IEnumerator<string>.Current => _inner.GetString(0);
    object IEnumerator.Current => _inner.GetString(0);

    internal ExamKeysIterator(SqliteDataReader reader)
    {
        _inner = reader;
    }
    
    public bool MoveNext() => _inner.Read();

    public void Reset() => throw new NotSupportedException();

    public void Dispose() => _inner.Dispose();
    
    public IEnumerator<string> GetEnumerator() => this;
    IEnumerator IEnumerable.GetEnumerator() => this;
}

/// Input: Array of obtained question results
/// Output: Array of integer values indicating how many points were scored for each question
public static class ExamResultObtainer
{
    public static CorrectAnswer[] ObtainCorrectAnswers(string examCode)
    {
        var correctAnswers = new List<CorrectAnswer>();
        var verifyCommand = Manager.Connection.CreateCommand();
        verifyCommand.CommandText = "SELECT AnswerNumber, QuestionId, QuestionNumber FROM Solutions WHERE Uuid == @Uuid ORDER BY QuestionNumber ASC;";
        verifyCommand.Parameters.AddWithValue("@Uuid", examCode);
        using var reader = verifyCommand.ExecuteReader();
        while (reader.Read())
        {
            var answerNumberBlob = reader.GetStream(0); // reader.GetInt32(0);
            var questionId = reader.GetInt32(1);
            var i = reader.GetInt32(2);

            var memoryStream = new MemoryStream();
            answerNumberBlob.CopyTo(memoryStream);
            var decryptedData = Encryption.DecryptBlob(memoryStream.ToArray());
            var answerNumber = BitConverter.ToInt64(decryptedData, 0);
            
            correctAnswers.Add(new CorrectAnswer(answerNumber, questionId));
            if (correctAnswers.Count != i + 1)
            {
                throw new MissingTaskFromDatabase(correctAnswers.Count);
            }
        }
        return [.. correctAnswers];
    }

    /// Get the number of questions the exam sheet with the provided exam id contains.
    /// <exception cref="InvalidExamQuestionCountException"></exception>
    public static uint ObtainExamQuestionCount(string examCode)
    {
        var uuidQuestionCountCommand = Manager.Connection.CreateCommand();
        uuidQuestionCountCommand.CommandText = "SELECT COUNT() FROM Solutions WHERE Uuid = @Uuid;";
        uuidQuestionCountCommand.Parameters.AddWithValue("@Uuid", examCode);
        if (uuidQuestionCountCommand.ExecuteScalar() is not long count) return 0;
        if (Clamp(count, 1, SheetTreeInfo.MaximumNumberOfQuestions) != count)
            throw new InvalidExamQuestionCountException("ObtainExamQuestionCount: The database contains an invalid number of exam questions for the provided index.");
        return (uint)count;
    }

    /// Get an iterator to every exam key.
    public static ExamKeysIterator ObtainExamKeys()
    {
        var getKeysCommand = Manager.Connection.CreateCommand();
        getKeysCommand.CommandText = "SELECT DISTINCT(Uuid) FROM Solutions ORDER BY LENGTH(Uuid) ASC, Uuid ASC;";
        var reader = getKeysCommand.ExecuteReader();
        return new ExamKeysIterator(reader);
    }
}