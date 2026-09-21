using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Threading;
using Docnet.Core.Models;
using Docnet.Core.Readers;
using Microsoft.Win32;
using OpenCvSharp;
using Scanner;
using Storage;

namespace Interface.Solve;

public partial class SolvePage
{
    private const int DimX = 1200;
    private const int DimY = 1700;
    
    private readonly BackgroundWorker _backgroundWorker = new();
    private ScanResult[] _csvBuffer = [];
    
    public SolvePage()
    {
        InitializeComponent();
        _backgroundWorker.ProgressChanged += BackgroundWorker_ProgressChanged;
        _backgroundWorker.DoWork += BackgroundLoader_DoWork;
        _backgroundWorker.RunWorkerCompleted += BackgroundLoader_RunWorkerCompleted;
    }

    private void ImportPdf_OnClick(object sender, RoutedEventArgs e)
    {
        string path = GetLoadPath(Interface.Resources.Lang.Browse_SolveImportTitle);
        if (string.IsNullOrEmpty(path)) 
            return;
        
        ProgressBar.Value = 0;
        ExportResultsButton.IsEnabled = false;
        ExportResultsAltButton.IsEnabled = false;
        ProgressLogs.Text = "";
        _backgroundWorker.WorkerReportsProgress = true;
        ProgressBar.Maximum = GetPageCount(path);
        _backgroundWorker.RunWorkerAsync(argument: path);
    }

    private void ExportResults_OnClick(object sender, RoutedEventArgs e)
    {
        var exportPath = GetSavePath(Interface.Resources.Lang.Browse_SolveExportTitle);
        if (string.IsNullOrEmpty(exportPath)) 
            return;
        try
        {
            var locale = GetLocaleSpecifics();
            var stringLines = _csvBuffer.Select((v, i) => v.ToSheetCsv(i, locale));
            var maxTaskCount = _csvBuffer.Select((v, _) => v.Results.Length).Max();
            var sb = GetStringFromDualStringBuilders(stringLines, GetCsvHeader(locale, maxTaskCount));
            File.WriteAllText(exportPath, sb.ToString(), Encoding.UTF8);
            MessageBox.Show(Interface.Resources.Lang.Export_ExportSaved, Interface.Resources.Lang.Export_WindowLabel, MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(Interface.Resources.Lang.Export_ExportFailed + "\n" + ex.Message, Interface.Resources.Lang.Export_WindowLabel, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ExportStats_OnClick(object sender, RoutedEventArgs e)
    {
        // TODO: This only works in cases where every test has the exact same tasks, just shuffled.
        var exportPath = GetSavePath(Interface.Resources.Lang.Browse_SolveExportTitle);
        if (string.IsNullOrEmpty(exportPath)) 
            return;
        try
        {
            var locale = GetLocaleSpecifics();
            var stringLines = _csvBuffer.Select((v, i) => v.ToTaskCsv(i, locale));
            var maxTaskIndex = _csvBuffer.Select((v, _) =>
                v.Results.Select(q => q.TaskIndex).Where(i => i is not null).Select(i => (int)i!).Max()).Max();
            var sb = GetStringFromDualStringBuilders(stringLines, GetCsvHeader(locale, maxTaskIndex));
            File.WriteAllText(exportPath, sb.ToString(), Encoding.UTF8);
            MessageBox.Show(Interface.Resources.Lang.Export_ExportSaved, Interface.Resources.Lang.Export_WindowLabel, MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(Interface.Resources.Lang.Export_ExportFailed + "\n" + ex.Message, Interface.Resources.Lang.Export_WindowLabel, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    private StringBuilder GetStringFromDualStringBuilders(IEnumerable<DualAutoSeparatedStringBuilder> stringLines, string header)
    {
        var text = new StringBuilder().Append(header);
        foreach (var line in stringLines)
        {
            text.Append(line);
            if (ExtraInfoCheckbox.IsChecked ?? false)
            {
                text.Append(line.ToExtraString());
            }
        }
        return text;
    }

    /// Return the header for the specified number of tasks.
    private static string GetCsvHeader(LocaleSpecifics locale, int taskCount)
    {
        var sb = new StringBuilder();
        var sep = locale.ListSeparator;
        sb.Append(Interface.Resources.Lang.Export_HeaderPage).Append(sep)
            .Append(Interface.Resources.Lang.Export_HeaderNeptun).Append(sep)
            .Append(Interface.Resources.Lang.Export_HeaderTotal).Append(sep)
            .Append(Interface.Resources.Lang.Export_HeaderSuccess);
        var taskTemplate = Interface.Resources.Lang.Export_HeaderTask;
        foreach (var taskNum in Enumerable.Range(0, taskCount))
        {
            sb.Append(sep).Append(taskTemplate.Replace("#NUM#", (taskNum + 1).ToString()));
        }
        return sb.Append('\n').ToString();
    }
    
    private static string GetLoadPath(string title)
    {
        OpenFileDialog openDialog = new OpenFileDialog
        {
            Filter = "Portable Document Format|*.pdf|All files|*.*",
            FilterIndex = 1,
            RestoreDirectory = true,
            Title = title
        };
        
        return openDialog.ShowDialog() == true ? Path.GetFullPath(openDialog.FileName) : string.Empty;
    }
    
    private static string GetSavePath(string title)
    {
        SaveFileDialog openDialog = new SaveFileDialog
        {
            Filter = "Comma Separated Values|*.csv|All files|*.*",
            FilterIndex = 1,
            RestoreDirectory = true,
            Title = title
        };
        
        return openDialog.ShowDialog() == true ? Path.GetFullPath(openDialog.FileName) : string.Empty;
    }
    
    private int GetPageCount(string path)
    {
        using var doclib = Docnet.Core.DocLib.Instance;
        using var reader = doclib.GetDocReader(path, new PageDimensions(DimX, DimY));
        return reader.GetPageCount();
    }

    private void BackgroundWorker_ProgressChanged(object? sender, ProgressChangedEventArgs e)
    {
        var scanResult = (ScanResult?)e.UserState;
        ProgressBar.Value = e.ProgressPercentage;
        ProgressLogs.Text += $"Page {e.ProgressPercentage}/{ProgressBar.Maximum}: {scanResult?.CurrentState}\n";
        ProgressLogsScroll.ScrollToEnd();
    }

    private void BackgroundLoader_DoWork(object? sender, DoWorkEventArgs e)
    {
        var path = (string)(e.Argument ?? "");
        
        using var doclib = Docnet.Core.DocLib.Instance;
        using var reader = doclib.GetDocReader(path, new PageDimensions(DimX, DimY));

        var pageCount = reader.GetPageCount();
        var scanResults = new ScanResult[pageCount];

        var skipDialog = false;
    
        for (var i = 0; i < pageCount; i++)
        {
            try
            {
                scanResults[i] = ScannerHandler.ScanPdfPage(reader, i);
            }
            catch (Exception ex)
            {
                Log.Write($"Unexpected exception while trying to check exam: {ex.Message}", Log.Severity.Error);
                scanResults[i] = new ScanResult
                {
                    CurrentState = ScanResult.State.UnexpectedException,
                    ExamCode = null,
                    UserCode = null,
                    FinalPoints = null,
                    Results = [],
                };
            }

            // If there is a missing code, open the dialog to fix it.
            if (!skipDialog
                && (scanResults[i].CurrentState == ScanResult.State.MissingExamCode
                || scanResults[i].CurrentState == ScanResult.State.MissingUserCode))
            {
                var pageImg = ScannerHandler.GetSinglePageAsBitmap(reader, i);
                scanResults[i] = DispatchHelpDialog(pageImg, scanResults[i], out skipDialog);
                
                try 
                {
                    HandleUnfinishedResults(ref scanResults[i], i, reader);
                }
                catch (Exception ex)
                {
                    Log.Write($"Unexpected exception while trying to check manually corrected exam: {ex.Message}", Log.Severity.Error);
                    scanResults[i].CurrentState = ScanResult.State.UnexpectedException;
                    scanResults[i].FinalPoints = null;
                    scanResults[i].Results = [];
                }
            }
            
            _backgroundWorker.ReportProgress(i + 1, scanResults[i]);
        }
        e.Result = scanResults;
    }

    private static ScanResult DispatchHelpDialog(Bitmap image, ScanResult invalidResult, out bool skipFuture)
    {
        bool skipFuturePrompts = false;
        ScanResult newResult = invalidResult;
        Application.Current.Dispatcher.Invoke(() =>
        {
            MissingCodeTool window = new MissingCodeTool
            {
                Owner = Application.Current.MainWindow,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                FixedResult = invalidResult,
            };
            window.Setup(image, invalidResult);
            window.ShowDialog();
            newResult = window.FixedResult;
            skipFuturePrompts = window.SkipFuturePrompts;
        });
        skipFuture = skipFuturePrompts;
        return newResult;
    }

    private static void HandleUnfinishedResults(ref ScanResult result, int i, Docnet.Core.Readers.IDocReader reader)
    {
        switch (result.CurrentState)
        {
            case ScanResult.State.MissingExamCode:
                return; // If still missing code, skip processing
            case ScanResult.State.ManuallyCorrected:
            {
                var pageImg = ScannerHandler.GetSinglePageAsBitmap(reader, i);
                result = ScannerHandler.ScanPageResults(pageImg, 5, result);
                break;
            }
        }
        ScannerHandler.ProcessScanResults(ref result, i);
    }

    private void BackgroundLoader_RunWorkerCompleted(object? sender, RunWorkerCompletedEventArgs e)
    {
        var results = (ScanResult[]?)e.Result;
        if (results == null || results.Length == 0)
        {
            return;
        }
        _csvBuffer = results;
        ExportResultsButton.IsEnabled = true;
        ExportResultsAltButton.IsEnabled = true;
    }

    private static LocaleSpecifics GetLocaleSpecifics()
    {
        return new LocaleSpecifics()
        {
            Culture = CultureInfo.CurrentCulture,
            DecimalPoint = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator,
            ListSeparator = CultureInfo.CurrentCulture.TextInfo.ListSeparator,
            SuccessText = "Sikeres", // TODO: Localize
            FailText = "Sikertelen", // TODO: Localize
        };
    }
}