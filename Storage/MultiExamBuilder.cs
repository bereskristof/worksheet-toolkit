using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;
using Storage.Sheet;
using Storage.Task;
using static Storage.PdfExportResult.Results;
using Skeleton = System.Tuple<long, long, long, System.Guid>;
using Skeletons = System.Collections.Generic.List<System.Tuple<long, long, long, System.Guid>>;

namespace Storage;

[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public static class MultiExamBuilder
{
    private const uint UuidMaxTries = 5;
    private static readonly string UuidErrorMessageTemplate = """
                                                       Failed to create unique uuid.

                                                       Only share this information with someone you trust to have database access!
                                                       Additional information:
                                                       - Uuids attempted:
                                                         - {0}
                                                         - {1}
                                                         - {2}
                                                         - {3}
                                                         - {4}
                                                       - Number of Uuids in the database already:
                                                         - {5}
                                                       """;
    
    /// Generates N exams based on the provided root node and answer count.
    /// Disables saving to database, since this is only used for previewing.
    public static LatexBuilder BuildNExams(uint n, SelectorNode rootNode, byte answerCount, string title, string author, string date, bool shuffleAnswers, string[] excludedAnswers)
    {
        if (n == 0)
            throw new ArgumentException("The number of exams to generate must be greater than zero.", nameof(n));
        
        var builder = new LatexBuilder(title, author, date);
        builder.AutoHeader();
        for (uint i = 0; i < n - 1; i++)
        {
            AddExam(builder, rootNode, answerCount, shuffleAnswers, excludedAnswers, false);
            builder.Macro("cleardoublepage");
            builder.Text(@"\pagestyle{plain}");
            builder.Text(@"\setcounter{page}{1}");
        }
        AddExam(builder, rootNode, answerCount, shuffleAnswers, excludedAnswers, false); // Add the last exam without a new page after it
        builder.AutoFooter();
        return builder;
    }
    
    public static void BeginManualAdding(LatexBuilder builder)
    {
        builder.AutoHeader();
    }

    public static void AddExam(LatexBuilder builder, SelectorNode rootNode, byte answerCount, bool shuffleAnswers, string[] excludedAnswers, bool storeSkeleton = true)
    {
        var questions = rootNode.GetQuestions();
        Skeletons skeleton = [];
        Guid uuid = CreateUniqueUuid();
        
        var humanReadableCode = MiniCodeGenerator.GenerateCode();
        
        var qrBuilder = new QrBuilder(uuid); // Create a QR code builder for the UUID
        var qrPath = Path.Combine(Path.GetTempPath(), Manager.PathTitle, $"{uuid.ToString()}.png");
        Directory.CreateDirectory(Path.GetDirectoryName(qrPath) ?? throw new InvalidOperationException("Invalid directory name in AddExam"));
        qrBuilder.Save(qrPath);
        var qrShortPath = Path.GetFileName(qrPath);
        
        builder.AddTitle(humanReadableCode);
        builder.Begin("questions");
        var i = 0;
        foreach (var question in questions)
        {
            AddQuestion(question, builder, skeleton, uuid, answerCount, i, shuffleAnswers, excludedAnswers);
            i++;
        }
        builder.End("questions");
        builder.Macro("cleardoublepage");
        builder.AddAnswerPage(questions.Length, answerCount, humanReadableCode, qrShortPath);
        builder.Macro("cleardoublepage");
        if (storeSkeleton)
            StoreSkeleton(skeleton);
    }

    /// Generate a new GUID that is checked for uniqueness.
    /// This is done because while a UUID should never clash in theory,
    /// on real systems, bad entropy can cause repeated values to appear exceedingly rarely.
    private static Guid CreateUniqueUuid()
    {
        List<Guid> failedUuids = [];
        for (uint attempt = 0; attempt < UuidMaxTries; attempt++)
        {
            Guid uncheckedUuid = Guid.NewGuid();
            var uuidVerifyCommand = Manager.Connection.CreateCommand();
            uuidVerifyCommand.CommandText = "SELECT COUNT(1) FROM Solutions WHERE Uuid = @Uuid;";
            uuidVerifyCommand.Parameters.AddWithValue("@Uuid", uncheckedUuid.ToString());
            long verifyValue = (long)(uuidVerifyCommand.ExecuteScalar() ?? throw new InvalidOperationException("Database didn't return whether the UUID is unique!"));
            bool isUnique = verifyValue == 0;
            if (isUnique)
            {
                return uncheckedUuid;
            }
            Log.Write($"UUID {uncheckedUuid.ToString()} failed to be unique!", Log.Severity.Warning);
            failedUuids.Add(uncheckedUuid);
        }

        var uuidCountCommand = Manager.Connection.CreateCommand();
        uuidCountCommand.CommandText = "SELECT COUNT(DISTINCT Uuid) FROM Solutions;";
        long uniqueUuidsInDb = (long)(uuidCountCommand.ExecuteScalar() ?? throw new InvalidOperationException("Database didn't return the number of unique UUIDs!"));
        throw new RepeatedlyRepeatingGuidException(
            string.Format(
                UuidErrorMessageTemplate,
                failedUuids[0],
                failedUuids[1],
                failedUuids[2],
                failedUuids[3],
                failedUuids[4],
                uniqueUuidsInDb));
    }
    
    public static void EndManualAdding(LatexBuilder builder)
    {
        builder.AutoFooter();
    }

    private static void AddQuestion(Question question, LatexBuilder builder, Skeletons skeleton, Guid uuid, byte answerCount, int questionIndex, bool shuffleAnswers, string[] excludedAnswers)
    {
        Answer[] answers;
        try
        {
            answers = question.Answers.GetRandomAnswers(answerCount, shuffleAnswers, excludedAnswers);
        }
        catch (InvalidOperationException)
        {
            throw new InvalidOperationException($"Question has no correct answer:\n\n{question.Id}. {question.Text}");
        }
        
        var answerIndex = answers
            .Select((ans, i) => new {ans, i})
            .Where(v => v.ans.Correct)
            .Select(v => v.i)
            .FirstOrDefault(); // Get the index of the first correct answer, or 0 if none are correct
        Skeleton skeletonElement = new Skeleton(questionIndex, answerIndex, question.Id, uuid); // Store IDs to allow for reconstruction
        skeleton.Add(skeletonElement);
        var path = ExportMaybeDuplicateQuestionImage(question);
        builder.Question(
            question.Text,
            question.Points, 
            answers.Select(a => a.Text).ToArray(),
            path
        );
    }

    private static string? ExportMaybeDuplicateQuestionImage(Question question)
    {
        var imageData = question.FetchImageStream()?.ToArray();
        if (imageData == null)
            return null; // No image to export
        var imagePath = Path.Combine(Path.GetTempPath(), Manager.PathTitle, $"q{question.Id}.png");
        Directory.CreateDirectory(Path.GetDirectoryName(imagePath) ?? throw new InvalidOperationException("Invalid directory name"));
        if (File.Exists(imagePath))
            return imagePath; // Image already exists, no need to fetch again
        File.WriteAllBytes(imagePath, imageData);
        return imagePath;
    }
    
    private static void StoreSkeleton(Skeletons skeletons)
    {
        foreach (var (question, answer, questionId, uuid) in skeletons)
        {
            var encryptedAnswer = Encryption.EncryptBlob(BitConverter.GetBytes(answer));
            var storeCommand = Manager.Connection.CreateCommand();
            storeCommand.CommandText = "INSERT INTO Solutions (Uuid, QuestionNumber, AnswerNumber, QuestionId) VALUES (@Uuid, @QuestionNumber, @AnswerNumber, @QuestionId);";
            storeCommand.Parameters.AddWithValue("@Uuid", uuid.ToString()); // Each exam gets a new UUID
            storeCommand.Parameters.AddWithValue("@QuestionNumber", question);
            storeCommand.Parameters.AddWithValue("@AnswerNumber", encryptedAnswer);
            storeCommand.Parameters.AddWithValue("@QuestionId", questionId); // Store the question ID for statistics
            storeCommand.ExecuteNonQuery();
        }
    }
    
    private static string ExportTex(LatexBuilder builder, Guid uuid)
    {
        var filename = Path.Combine(Path.GetTempPath(), Manager.PathTitle, $"{uuid.ToString()}.tex");
        using var writer = new StreamWriter(filename);
        writer.Write(builder.Finish());
        writer.Close();
        ExportArUcos();
        return filename;
    }

    public static PdfExportResult TryExportPdf(LatexBuilder builder)
    {
        var batchUuid = Guid.NewGuid();
        var filename = ExportTex(builder, batchUuid);
        try
        {
            CallPdfLatex(filename);
            CallPdfLatex(filename); // Latex "ain't the best", so we have to call it twice
        }
        catch (TimeoutException e)
        {
            Log.Write($"ExportPdf: Failed to export PDF: {e.Message}", Log.Severity.Error);
            return new PdfExportResult(ErrorWithMessage, "Timeout export failed: " + e.Message);
        }
        catch (Win32Exception e)
        {
            Log.Write($"ExportPdf: Failed to export PDF: {e.Message}", Log.Severity.Error);
            if (e.NativeErrorCode == 2)
                return new PdfExportResult(ExecutableInaccessible);
            return new  PdfExportResult(ErrorWithMessage, "Win32 error: " + e.Message);
        }
        var pdfPath = Path.ChangeExtension(filename, ".pdf");
        Log.Write($"ExportPdf: Exported PDF to {pdfPath}");
        return new PdfExportResult(Success, pdfPath);
    }
    
    private static void CallPdfLatex(string texPath)
    {
        var process = new Process();
        var flags = $"-halt-on-error -output-directory=\"{Path.GetDirectoryName(texPath)}\" \"{texPath}\"";
        process.StartInfo = new ProcessStartInfo(TexDoctor.GetTexPath("pdflatex"), flags)
            { CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
        process.Start();
        var stdout = process.StandardOutput.ReadToEnd(); // Latex very helpfully puts its error messages to stdout, not to stderr
        var finished = process.WaitForExit(30_000); // Wait for 30 seconds for the process to complete
        var outputPath = Path.ChangeExtension(texPath, ".pdf");
        if (finished && process.ExitCode == 0 && File.Exists(outputPath)) return;
        process.Kill(true);
        var killed = process.WaitForExit(5_000);
        if (!killed)
        {
            Log.Write($"ExportPdf: pdflatex failed to complete, child process has refused to be killed, ABANDONING!", Log.Severity.Error);
            Environment.Exit(-90); // <--- Process was force abandoned due to extreme complications
        }

        Log.Write($"ExportPdf: pdflatex failed to complete, child process was killed", Log.Severity.Warning);
        var errorCapture = Regex.Match(stdout, "!((?:.|\\n)*?)! *==>"); // Pretty naive regex to capture errors from stdout
        var errorText = string.Join(" ", errorCapture.Groups.Cast<Group>().Skip(1).Select(g => g.Value));
        throw new TimeoutException(errorText);
    }

    private static void ExportArUcos()
    {
        for (int i = 0; i < 4; i++)
        {
            var fileName = $"aruco_{i}.png";
            var filePath = Path.Combine(Path.GetTempPath(), Manager.PathTitle, fileName);
            var resourcePath = $"Storage.ArUco.{fileName}";
            ExportFromAssembly(resourcePath, filePath);
        }
    }
    
    private static void ExportFromAssembly(string resourceName, string outputPath)
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
            throw new InvalidOperationException($"Resource {resourceName} not found in assembly.");
        
        using var fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write);
        stream.CopyTo(fileStream);
    }

    /// Cleans up all of temp/.
    public static void CleanUp(string exportPath)
    {
        var dir = new FileInfo(exportPath).Directory!.FullName;
        var files = Directory.GetFiles(dir);
        foreach (var file in files)
            File.Delete(file);
    }
}