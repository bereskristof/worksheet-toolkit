using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using Scanner.Collect;
using Scanner.Managing;

namespace Interface.Solve;

public partial class MissingCodeTool
{
    public required MissingData MissingData;
    public AnyData ReturnAnyData;
    
    public bool SkipFutureExamPrompts = false;
    public bool SkipFutureUserPrompts = false;
    
    public MissingCodeTool()
    {
        InitializeComponent();
    }

    public void Setup()
    {
        ReturnAnyData = MissingData;
        PreviewImage.Source = ConvertBitmapToImageSource(MissingData.State.PageBitmap);
        ExamTextBox.Text = MissingData.State.AutoExamCode.Replace("-", "");
        CodeTextBox.Text = MissingData.State.AutoUserCode;
        if (!string.IsNullOrEmpty(MissingData.UserCode.Code))
            SkipUser.Visibility = Visibility.Collapsed;
        if (!string.IsNullOrEmpty(MissingData.ExamCode.Code))
            SkipExam.Visibility = Visibility.Collapsed;
        ExamTextBox.IsEnabled = !MissingData.State.IsExamAlreadySolved;
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
        if (ExamCode.Create(examCodeStr).TryValidate() is { } examCode) 
        {
            MissingData.ExamCode = examCode;
        }
        else
        {
            MessageBox.Show(Interface.Resources.Lang.Missing_ExamCode, Interface.Resources.Lang.Common_Error, MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
        
        var userCodeStr = CodeTextBox.Text.Trim();
        if (UserCode.Create(userCodeStr).TryValidate() is { } userCode)
        {
            MissingData.UserCode = userCode;
        }
        else
        {
            MessageBox.Show(Interface.Resources.Lang.Missing_Neptun, Interface.Resources.Lang.Common_Error, MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
        
        ReturnAnyData = MissingData.Validate(examCode, userCode);
        Close();
    }

    private void SkipButton_OnClick(object sender, RoutedEventArgs e)
    {
        ReturnAnyData = MissingData;
        Close();
    }

    private void SkipUser_OnChecked(object sender, RoutedEventArgs e) => SkipFutureUserPrompts = true;

    private void SkipUser_OnUnchecked(object sender, RoutedEventArgs e) => SkipFutureUserPrompts = false;

    private void SkipExam_OnChecked(object sender, RoutedEventArgs e) => SkipFutureExamPrompts = true;

    private void SkipExam_OnUnchecked(object sender, RoutedEventArgs e) => SkipFutureExamPrompts = false;
}