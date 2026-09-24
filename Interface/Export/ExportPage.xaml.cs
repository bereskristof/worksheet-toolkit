using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Docnet.Core.Models;
using Microsoft.Win32;
using Storage;
using PixelFormat = System.Drawing.Imaging.PixelFormat;

using WorkArgs = System.Tuple<string, string, string>;
using PreviewTuple = System.Tuple<string?, Interface.Export.ExportPage.PageData[], double, double, string>;

namespace Interface.Export;

public partial class ExportPage : INotifyPropertyChanged
{
    private enum ShuffleMode
    {
        Yes,
        No,
        Smart,
    }
    
    private ShuffleMode _shuffleMode = ShuffleMode.Smart;
    
    private string[] _smartExclusions = [];
    
    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged(string propertyName)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    
    public struct PageData
    {
        public byte[] Data;
        public int Width;
        public int Height;
        public double Y;
    }
    
    private readonly ScaleTransform _scaleTransform = new();
    
    private int _dimX = 1080;
    private int _dimY = 1920;
    
    private uint _pageCount = 1;
    public uint PageCount
    {
        get => _pageCount;
        set
        {
            if (_pageCount == value) return;
            _pageCount = value;
            OnPropertyChanged(nameof(PageCount));
        }
    }
    
    private byte _answerCount = 5;
    public byte AnswerCount
    {
        get => _answerCount;
        set
        {
            if (_answerCount == value) return;
            _answerCount = value;
            OnPropertyChanged(nameof(AnswerCount));
        }
    }
    
    private string _exportPath = "";

    public string ExportPath
    {
        get => _exportPath;
        set
        {
            if (_exportPath == value) return;
            _exportPath = value;
            OnPropertyChanged(nameof(ExportPath));
        }
    }
    
    public ExportPage()
    {
        InitializeComponent();
        var pageCountBinding = new Binding("PageCount")
        {
            Source = this,
            Path = new PropertyPath("PageCount"), 
            Mode = BindingMode.TwoWay,
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
        };
        var exportPathBinding = new Binding("ExportPath")
        {
            Source = this,
            Path = new PropertyPath("ExportPath"),
            Mode = BindingMode.TwoWay,
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
        };
        BindingOperations.SetBinding(AmountBox, TextBox.TextProperty, pageCountBinding);
        BindingOperations.SetBinding(ExportBox, TextBox.TextProperty, exportPathBinding);
        PreviewScroll.RenderTransform = _scaleTransform;
    }

    private void ButtonBase_OnClick(object sender, RoutedEventArgs e) 
        => CreatePdfPreview();

    private void ZoomOut_OnClick(object sender, RoutedEventArgs e)
        => RescalePreview(0.8);

    private void ZoomIn_OnClick(object sender, RoutedEventArgs e)
        => RescalePreview(1.25);

    private void ScaleSmall_OnSelect(object sender, RoutedEventArgs e)
    {
        _dimX = 540;
        _dimY = 960;
    }

    private void ScaleMedium_OnSelect(object sender, RoutedEventArgs e)
    {
        _dimX = 1080;
        _dimY = 1920;
    }

    private void ScaleLarge_OnSelect(object sender, RoutedEventArgs e)
    {
        _dimX = 2160;
        _dimY = 3840;
    }

    private void CreatePdfPreview()
    {
        var blocking = Exporting.IsTreeSafe(Bindings.Instance.SheetRoot);
        if (blocking == Exporting.TreeSafetyResult.Blocked)
            return;
        
        CreateButton.IsEnabled = false;
        PreviewProgressBar.Visibility = Visibility.Visible;
        PreviewScroll.Children.Clear();
        _scaleTransform.ScaleX = 1.0;
        _scaleTransform.ScaleY = 1.0;
        BackgroundWorker previewWorker = new();
        previewWorker.DoWork += BackgroundLoader_DoWork;
        previewWorker.RunWorkerCompleted += BackgroundLoader_RunWorkerCompleted;
        var title = TitleBox.Text.Trim();
        var author = AuthorBox.Text.Trim();
        var date = DateBox.Text.Trim();
        var args = new WorkArgs(title, author, date);
        previewWorker.RunWorkerAsync(argument: args);
    }
    
    private void BackgroundLoader_DoWork(object? sender, DoWorkEventArgs e)
    {
        var (title, author, date) = (WorkArgs)e.Argument!;
        
        // Create a PDF to preview
        LatexBuilder builder;
        try
        {
            GetShuffleVars(out var shuffle, out var exclusions);
            builder = MultiExamBuilder.BuildNExams(1, Bindings.Instance.SheetRoot, AnswerCount, title, author, date, shuffle, exclusions);
        }
        catch (InvalidOperationException ex)
        {
            e.Result = new PreviewTuple(ex.Message, [], 0, 0, "");
            return;
        }
        var result = MultiExamBuilder.TryExportPdf(builder);
        switch (result.Result)
        {
            case PdfExportResult.Results.ExecutableInaccessible:
                e.Result = new PreviewTuple(Interface.Resources.Lang.Export_TexInaccessible, [], 0, 0, string.Empty);
                return;
            case PdfExportResult.Results.ErrorWithMessage:
                e.Result = new PreviewTuple(result.Message, [], 0, 0, string.Empty);
                return;
            case PdfExportResult.Results.Success:
                break;
        }
        
        // Preview the PDF file
        using var doclib = Docnet.Core.DocLib.Instance;
        using var reader = doclib.GetDocReader(result.Message, new PageDimensions(_dimX, _dimY));

        var pageCount = reader.GetPageCount();

        var canvasYOffset = 20;
        var canvasXOffset = 0;
        
        PageData[] pages = new PageData[pageCount];
        
        for (var i = 0; i < pageCount; i++)
        {
            using var pageReader = reader.GetPageReader(i);
            var page = pageReader.GetImage();
            
            var width = pageReader.GetPageWidth();
            var height = pageReader.GetPageHeight();
            
            using var bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            
            var bmpData = bmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            System.Runtime.InteropServices.Marshal.Copy(page, 0, bmpData.Scan0, page.Length);
            bmp.UnlockBits(bmpData);
            
            var stream = new MemoryStream();
            bmp.Save(stream, ImageFormat.Png);
            
            var pageData = new PageData
            {
                Data = stream.ToArray(),
                Width = width,
                Height = height,
                Y = canvasYOffset,
            };
            pages[i] = pageData;
            
            canvasYOffset += height + 22; // 20px margin + 2px border
            canvasXOffset = int.Max(width + 2, canvasXOffset);
        }

        e.Result = new PreviewTuple(null, pages, canvasYOffset, canvasXOffset, result.Message);
    }

    private void BackgroundLoader_RunWorkerCompleted(object? sender, RunWorkerCompletedEventArgs e)
    {
        var (error, pages, canvasYOffset, canvasXOffset, pdfPath) = (PreviewTuple)e.Result!;
        if (error != null)
        {
            MessageBox.Show(error, Interface.Resources.Lang.Common_Error, MessageBoxButton.OK, MessageBoxImage.Error);
            PreviewProgressBar.Visibility = Visibility.Collapsed;
            CreateButton.IsEnabled = true;
            return;
        }

        foreach (var page in pages)
        {
            var previewImage = new LatexPagePreview();
            var stream = new MemoryStream();
            stream.Write(page.Data, 0, page.Data.Length);
            previewImage.MainImage.Source = BitmapFrame.Create(stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
            previewImage.MainImage.Width = page.Width;
            previewImage.MainImage.Height = page.Height;
            Canvas.SetTop(previewImage, page.Y);
            Canvas.SetLeft(previewImage, 20);
            PreviewScroll.Children.Add(previewImage);
        }

        PreviewScroll.Width = canvasXOffset + 40; // 20 px margin on the right
        PreviewScroll.Height = canvasYOffset;
        PreviewProgressBar.Visibility = Visibility.Collapsed;
        CreateButton.IsEnabled = true;
        MultiExamBuilder.CleanUp(pdfPath);
    }

    private void RescalePreview(double mult)
    {
        _scaleTransform.ScaleX *= mult;
        _scaleTransform.ScaleY *= mult;
        PreviewScroll.Width *= mult;
        PreviewScroll.Height *= mult;
    }

    private void ExportButton_OnClick(object sender, RoutedEventArgs e)
    {
        var window = new ExportingWindow
        {
            Owner = Window.GetWindow(this),
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Title = Interface.Resources.Lang.Solve_ExportingPdf,
        };
        var title = TitleBox.Text.Trim();
        var author = AuthorBox.Text.Trim();
        var date = DateBox.Text.Trim();
        GetShuffleVars(out var shuffle, out var exclusions);
        var succeed = window.ExportNPages(Bindings.Instance.SheetRoot, PageCount, 5, title, author, date, ExportPath, shuffle, exclusions);
        if (succeed)
            window.ShowDialog();
    }

    private void BrowseButton_OnClick(object sender, RoutedEventArgs e)
    {
        var path = GetSavePath(Interface.Resources.Lang.Browse_ExportTitle);
        if (!string.IsNullOrEmpty(path))
            ExportPath = path;
    }
    
    private static string GetSavePath(string title)
    {
        SaveFileDialog saveDialog = new SaveFileDialog
        {
            Filter = "Portable Document Format|*.pdf|All files|*.*", // TODO: Allow tex?
            FilterIndex = 1,
            RestoreDirectory = true,
            Title = title
        };
        
        return saveDialog.ShowDialog() == true ? Path.GetFullPath(saveDialog.FileName) : string.Empty;
    }

    private void ShuffleOrder_OnSelected(object sender, RoutedEventArgs e)
        => SetShuffleMode(ShuffleMode.Yes);

    private void KeepOrder_OnSelected(object sender, RoutedEventArgs e)
        => SetShuffleMode(ShuffleMode.No);
    
    private void SmartShuffle_OnSelected(object sender, RoutedEventArgs e)
        => SetShuffleMode(ShuffleMode.Smart);

    private void SetShuffleMode(ShuffleMode mode)
    {
        _shuffleMode = mode;
        if (ExceptionList == null)
            return;
        var visibility = mode switch
        {
            ShuffleMode.Smart => Visibility.Visible,
            _ => Visibility.Collapsed,
        };
        ExceptionLabel.Visibility = visibility;
        ExceptionList.Visibility = visibility;
    }
    
    private void GetShuffleVars(out bool shuffle, out string[] shuffleExclusions)
    {
        switch (_shuffleMode)
        {
            case ShuffleMode.Yes:
                shuffle = true;
                shuffleExclusions = [];
                break;
            case ShuffleMode.No:
                shuffle = false;
                shuffleExclusions = [];
                break;
            case ShuffleMode.Smart:
                shuffle = true;
                shuffleExclusions = _smartExclusions;
                break;
            default:
                throw new UnreachableException("GetShuffleVars UnreachableException reached");
        }
    }

    private void ExceptionList_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        var exclusions = ExceptionList.Text
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrEmpty(s))
            .ToArray();
        _smartExclusions = exclusions;
    }
}