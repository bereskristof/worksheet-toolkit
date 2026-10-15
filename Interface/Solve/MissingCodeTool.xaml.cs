using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using Scanner.Codes;

namespace Interface.Solve;

public partial class MissingCodeTool
{
    public required MissingDataEntry MissingDataEntry;
    public bool SkipFuturePrompts = false;
    
    public MissingCodeTool()
    {
        InitializeComponent();
    }

    public void Setup()
    {
        PreviewImage.Source = ConvertBitmapToImageSource(MissingDataEntry.PageBitmap);
        ExamTextBox.Text = MissingDataEntry.ExamCode?.ToString().Replace("-", "") ?? string.Empty;
        CodeTextBox.Text = MissingDataEntry.UserCode?.ToString() ?? string.Empty;
    }
    
    private static BitmapImage ConvertBitmapToImageSource(Bitmap bitmap)
    {
        using var memoryStream = new MemoryStream();
        bitmap.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);
        memoryStream.Position = 0;

        var bitmapImage = new BitmapImage();
        bitmapImage.BeginInit();
        bitmapImage.StreamSource = memoryStream;
        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
        bitmapImage.EndInit();
        return bitmapImage;
    }

    private void EnterButton_OnClick(object sender, RoutedEventArgs e)
    {
        var examCodeStr = ExamTextBox.Text.Trim();
        if (ExamCode.TryCreate(examCodeStr) is { } examCode) 
        {
            MissingDataEntry.ExamCode = examCode.Inner;
        }
        else
        {
            MessageBox.Show(Interface.Resources.Lang.Missing_ExamCode, Interface.Resources.Lang.Common_Error, MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
        
        var userCodeStr = CodeTextBox.Text.Trim();
        if (UserCode.TryCreate(userCodeStr) is { } userCode)
        {
            MissingDataEntry.UserCode = userCode;
        }
        else
        {
            MessageBox.Show(Interface.Resources.Lang.Missing_Neptun, Interface.Resources.Lang.Common_Error, MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
        
        Close();
    }

    private void SkipButton_OnClick(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void ToggleButton_OnChecked(object sender, RoutedEventArgs e) => SkipFuturePrompts = true;

    private void ToggleButton_OnUnchecked(object sender, RoutedEventArgs e) => SkipFuturePrompts = false;
}