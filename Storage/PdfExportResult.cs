namespace Storage;

public record PdfExportResult(PdfExportResult.Results Result, string Message = "")
{
    public enum Results
    {
        /// File was exported successfully, message is the export path.
        Success,
        /// PdfLaTeX was inaccessible, message is meaningless.
        ExecutableInaccessible,
        /// A generic error occured, message is related to the error.
        ErrorWithMessage,
    }
    
    public Results Result = Result;
    public string Message = Message;
}