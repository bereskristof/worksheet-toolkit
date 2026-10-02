using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Docnet.Core.Readers;
using OpenCvSharp;
using Scanner;
using Scanner.Result;
using Storage;

namespace Interface.Solve;

public static class ScannerHandler
{
    public static ScanResult ScanPdfPage(IDocReader reader, int i)
    {
        var pageImg = GetSinglePageAsBitmap(reader, i);
        var scanResult = ScanPageResults(pageImg, 5);
        return scanResult;
    }
    
    public static Bitmap GetSinglePageAsBitmap(IDocReader reader, int pageIndex)
    {
        using var pageReader = reader.GetPageReader(pageIndex);
        var page = pageReader.GetImage();
            
        var width = pageReader.GetPageWidth();
        var height = pageReader.GetPageHeight();
            
        var bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            
        var bmpData = bmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
        System.Runtime.InteropServices.Marshal.Copy(page, 0, bmpData.Scan0, page.Length);
        bmp.UnlockBits(bmpData);
            
        var stream = new MemoryStream();
        bmp.Save(stream, ImageFormat.Bmp);
        return bmp;
    }
    
    public static ScanResult ScanPageResults(Bitmap pageImg, uint answerCount, ScanResult? fixedResult = null)
    {
        var result = new ScanResult();
        result.Results = [];
        var imageConverter = new ImageConverter();
        var imageData = (byte[])(imageConverter.ConvertTo(pageImg, typeof(byte[])) ?? Array.Empty<byte>());
        if (fixedResult == null)
        {
            var qrScanner = new CodeScanner(imageData);
            qrScanner.FindCodes(ref result);
            if (result.ExamCode.Get() == null)
            {
                return result;
            }
        }
        else
        {
            result = fixedResult.Value;
        }

        uint questionCount;
        try
        {
            questionCount = ExamResultObtainer.ObtainExamQuestionCount((Guid)result.ExamCode.Get()!);
        }
        catch (InvalidExamQuestionCountException)
        {
            result.Issues.Set(result.Issues.Get() | ScanResult.IssueFlags.UnreliableDataFromDatabase);
            return result;
        }

        CircleSegment[] matrixBubbles;
        try
        {
            var matrixScanner = new MatrixScanner(imageData);
            matrixBubbles = matrixScanner.FindBubbles(questionCount, answerCount);
        }
        catch (MarkerException)
        {
            result.Issues.Set(result.Issues.Get() | ScanResult.IssueFlags.MarkerDetectionError);
            return result;
        }
        
        var bubbleChecker = new BubbleChecker(imageData);
        var matrixResults = bubbleChecker.CheckBubbles(matrixBubbles, questionCount, answerCount);

        result.Results = new QuestionResult[questionCount];

        for (int i = 0; i < matrixResults.Length; i++)
        {
            var (mResult, mBest, mNextBest) = matrixResults[i];
            result.Results[i] = new QuestionResult
            {
                TaskIndex = RollbackConversions.GetNullInt(),
                BestFilledAnswer = mResult,
                DeltaConfidence = double.IsNaN((mBest - mNextBest) / mBest) ? 1.0 : ((mBest - mNextBest) / mBest), // [0, 1], since mNextBest <= mBest
                FillConfidence = mBest, // [0, 1], since it's a ratio
                Points = RollbackConversions.GetNullInt(),
            };
        }

        return result;
    }

    public static void ProcessScanResults(ref ScanResult result, int pageIndex)
    {
        for (int i = 0; i < result.Results.Length; i++)
        {
            var res = result.Results[i];
            if (res.FillConfidence < QuestionResult.MinFillConfidence)
            {
                result.Results[i].Points = QuestionResult.EmptyAnswerPoints.ToNullableRollback();
            }
            else if (res.DeltaConfidence <= QuestionResult.MinDeltaConfidence)
            {
                result.Results[i].Points = QuestionResult.WrongAnswerPoints.ToNullableRollback();
            }
            // Else: Answer is filled correctly. Points will be assigned later, and as such, is left as null for now.
        }

        ExamResultObtainer.ObtainResults(ref result);
    }
    
    public enum CodeType
    {
        ExamCode,
        UserCode
    }

    public static bool TryUpdateResult(ref ScanResult result, string newCode, CodeType newType)
    {
        switch (newType)
        {
            case CodeType.ExamCode:
                if (!CodeScanner.IsUuid(newCode)) return false;
                var uuid = Guid.ParseExact(newCode, "N");
                result.ExamCode.Set(uuid);
                return true;
            case CodeType.UserCode:
                if (!CodeScanner.IsNeptunCode(newCode)) return false;
                result.UserCode.Set(newCode);
                return true;
            default:
                return false;
        }
    }
}