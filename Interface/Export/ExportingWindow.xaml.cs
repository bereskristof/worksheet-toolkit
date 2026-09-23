using System.ComponentModel;
using System.IO;
using System.Windows;
using Storage;
using Storage.Sheet;
using WorkArgsInner = System.Tuple<string, bool, string[]>;
using WorkArgs = System.Tuple<string, Storage.Sheet.SelectorNode, byte, uint, string, string, System.Tuple<string, bool, string[]>>;

namespace Interface.Export;

public partial class ExportingWindow
{
    private readonly BackgroundWorker _backgroundWorker = new();
    
    public ExportingWindow()
    {
        InitializeComponent();
        WindowStyle = WindowStyle.None;
    }
    
    public bool ExportNPages(SelectorNode root, uint examCount, byte answerCount, string title, string author, string date, string target, bool shuffleAnswers, string[] excludedAnswers)
    {
        if (!CanCreateFile(target))
        {
            MessageBox.Show(Interface.Resources.Lang.Export_InvalidPath, Interface.Resources.Lang.Common_Error, MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }
        var blocking = Exporting.IsTreeSafe(root);
        if (blocking == Exporting.TreeSafetyResult.Blocked)
            return false;
        
        _backgroundWorker.WorkerReportsProgress = true;
        ExportProgressBar.Maximum = CalculateMaxProgress(examCount);
        Counter.Content = $"0 / {ExportProgressBar.Maximum}";
        _backgroundWorker.ProgressChanged += BackgroundWorker_ProgressChanged;
        _backgroundWorker.DoWork += BackgroundLoader_DoWork;
        _backgroundWorker.RunWorkerCompleted += BackgroundLoader_RunWorkerCompleted;
        var args = new WorkArgs(target, root, answerCount, examCount, title, author, new WorkArgsInner(date, shuffleAnswers, excludedAnswers));
        _backgroundWorker.RunWorkerAsync(argument: args);
        return true;
    }

    private static bool CanCreateFile(string target)
    {
        try
        {
            var fs = new FileStream(target, FileMode.Create, FileAccess.Write);
            fs.WriteByte(0);
            fs.Close();
            File.Delete(target);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static int CalculateMaxProgress(uint examCount)
        => (int)examCount;

    private void BackgroundWorker_ProgressChanged(object? sender, ProgressChangedEventArgs e)
    {
        if (Math.Abs(e.ProgressPercentage - ExportProgressBar.Maximum) < 1e-6)
        {
            ExportProgressBar.IsIndeterminate = true;
        }
        ExportProgressBar.Value = e.ProgressPercentage;
        Counter.Content = $"{e.ProgressPercentage} / {ExportProgressBar.Maximum}";
    }

    private void BackgroundLoader_DoWork(object? sender, DoWorkEventArgs e)
    {
        var (target, root, answerCount, examCount, title, author, inner) = (WorkArgs)e.Argument!;
        var (date, shuffleAnswers, excludedAnswers) = inner;
        
        var latexBuilder = new LatexBuilder(title, author, date);
        MultiExamBuilder.BeginManualAdding(latexBuilder);
        for (uint i = 0; i < examCount - 1; i++)
        {
            try
            {
                MultiExamBuilder.AddExam(latexBuilder, root, answerCount, shuffleAnswers, excludedAnswers);
            }
            catch (InvalidOperationException ex)
            {
                e.Result = ex.Message;
                return; // Stop the export if an error occurs
            }
            latexBuilder.Macro("cleardoublepage");
            latexBuilder.Text(@"\pagestyle{plain}");
            latexBuilder.Text(@"\setcounter{page}{1}");
            _backgroundWorker.ReportProgress((int)(i + 1));
        }
        try
        {
            MultiExamBuilder.AddExam(latexBuilder, root, answerCount, shuffleAnswers, excludedAnswers); // Add the last exam without a new page after it
        }
        catch (InvalidOperationException ex)
        {
            e.Result = ex.Message;
            return; // Stop the export if an error occurs
        }
        _backgroundWorker.ReportProgress((int)examCount);
        MultiExamBuilder.EndManualAdding(latexBuilder);
        
        var result = MultiExamBuilder.TryExportPdf(latexBuilder);
        var success = result.Result == PdfExportResult.Results.Success;
        if (success)
        {
            var exportPath = result.Message;
            File.Move(exportPath, target, true);
            MultiExamBuilder.CleanUp(exportPath);
        }
        e.Result = result.Result switch
        {
            PdfExportResult.Results.Success => null,
            PdfExportResult.Results.ExecutableInaccessible => Interface.Resources.Lang.Export_TexInaccessible,
            PdfExportResult.Results.ErrorWithMessage => result.Message,
            _ => throw new ArgumentOutOfRangeException(),
        };
    }

    private void BackgroundLoader_RunWorkerCompleted(object? sender, RunWorkerCompletedEventArgs e)
    {
        string? result = (string?)e.Result;
        if (result != null)
        {
            MessageBox.Show(result, Interface.Resources.Lang.Common_Error, MessageBoxButton.OK, MessageBoxImage.Error);
        }
        else
        {
            MessageBox.Show(Interface.Resources.Lang.Export_Success, Interface.Resources.Lang.Export_WindowLabel, MessageBoxButton.OK, MessageBoxImage.Information);
        }
        Close();
    }
}