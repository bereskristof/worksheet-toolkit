using System.Diagnostics;
using OpenCvSharp;
using OpenCvSharp.Aruco;

namespace Scanner;

public class MatrixScanner
{
    private const float WidthToCircleDiameterRatio = 0.064f;

    private readonly Mat _grayImage = new();
    
    public MatrixScanner(byte[] imageData)
    {
        var originalImage = Cv2.ImDecode(imageData, ImreadModes.Color);
        Cv2.CvtColor(originalImage, _grayImage, ColorConversionCodes.BGR2GRAY);
    }

    /// <exception cref="MarkerException">Failed to find at least 3 markers, or a marker was repeated</exception>
    public CircleSegment[] FindBubbles(uint questionCount = 15, uint answerCount = 5)
    {
        var dict = CvAruco.GetPredefinedDictionary(PredefinedDictionaryType.Dict4X4_50);
        var detector = new ArucoDetector(dict);
        detector.DetectMarkers(_grayImage, out var corners, out var ids, out _);

        var maybeAnswerMatrixCorners = GetMaybeMarkerCenters(corners, ids);
        var answerMatrixCorners = GetAnswerMatrixCorners(maybeAnswerMatrixCorners);
        var circles = GetAnswerBubbles(answerMatrixCorners, questionCount, answerCount);
        
        return circles;
    }

    private static Point2f?[] GetMaybeMarkerCenters(Point2f[][] corners, int[] ids)
    {
        int[] idToPosition = [3, 2, 0, 1];
        var maybeAnswerMatrixCorners = new Point2f?[4];
        for (uint i = 0; i < ids.Length; i++)
        {
            if (ids[i] >= idToPosition.Length)
                continue;
            
            float x = 0;
            float y = 0;
            for (uint j = 0; j < 4; j++)
            {
                x += corners[i][j].X;
                y += corners[i][j].Y;
            }
            x /= 4;
            y /= 4;
            
            int index = idToPosition[ids[i]];
            if (maybeAnswerMatrixCorners[index] != null)
                throw new MarkerException("Duplicate marker indices detected");
            maybeAnswerMatrixCorners[index] = new Point2f(x, y);
        }

        return maybeAnswerMatrixCorners;
    }
    
    private static Point2f[] GetAnswerMatrixCorners(Point2f?[] maybeAnswerMatrixCorners)
    {
        var answerMatrixCorners = new Point2f[4];
        for (int i = 0; i < 4; i++)
        {
            var corner = maybeAnswerMatrixCorners[i];
            if (corner == null)
            {
                answerMatrixCorners[i] = i switch // 0 1
                {                                 // 2 3
                    0 => RecoverPoint(maybeAnswerMatrixCorners[1], maybeAnswerMatrixCorners[2], maybeAnswerMatrixCorners[3]),
                    1 => RecoverPoint(maybeAnswerMatrixCorners[0], maybeAnswerMatrixCorners[3], maybeAnswerMatrixCorners[2]),
                    2 => RecoverPoint(maybeAnswerMatrixCorners[3], maybeAnswerMatrixCorners[0], maybeAnswerMatrixCorners[1]),
                    3 => RecoverPoint(maybeAnswerMatrixCorners[2], maybeAnswerMatrixCorners[1], maybeAnswerMatrixCorners[0]),
                    _ => throw new UnreachableException("GetAnswerMatrixCorners")
                };
                continue;
            }
            answerMatrixCorners[i] = corner.Value;
        }
        return answerMatrixCorners;
    }

    private static Point2f RecoverPoint(Point2f? uAligned, Point2f? vAligned, Point2f? diagonalAligned)
    {
        if (uAligned == null || vAligned == null || diagonalAligned == null)
        {
            throw new MarkerException("Cannot recover point, multiple points are null");
        }
        
        Point2f u = uAligned.Value - diagonalAligned.Value;
        Point2f v = vAligned.Value - diagonalAligned.Value;
        Point2f recoveredPoint = diagonalAligned.Value + u + v;
        return recoveredPoint;
    }

    private static CircleSegment[] GetAnswerBubbles(Point2f[] markerCenters, uint questionCount, uint answerCount)
    {
        CircleSegment[] circles = new CircleSegment[questionCount * answerCount];
        Point2f u = markerCenters[1] - markerCenters[0]; // / (answerCount + 1)
        Point2f v = markerCenters[2] - markerCenters[0]; // / (questionCount + 1)
        u.X /= answerCount + 1;
        u.Y /= answerCount + 1;
        v.X /= questionCount + 1;
        v.Y /= questionCount + 1;
        Point2f origin = markerCenters[0] + u + v;
        Point2f width = markerCenters[1] - markerCenters[0];
        float radius = MathF.Sqrt(width.X * width.X + width.Y * width.Y) * WidthToCircleDiameterRatio / 2f;
        for (int uMul = 0; uMul < answerCount; uMul++)
        {
            for (int vMul = 0; vMul < questionCount; vMul++)
            {
                var position = origin + u * uMul + v * vMul;
                circles[uMul + vMul * (int)answerCount] = new CircleSegment(position, radius);
            }
        }
        return circles;
    }
}