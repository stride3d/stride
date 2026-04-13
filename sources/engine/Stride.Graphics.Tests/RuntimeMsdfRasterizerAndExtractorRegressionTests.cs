using System;
using System.Linq;
using System.Runtime.InteropServices;
using Stride.Core.Mathematics;
using Stride.Core.Serialization.Contents;
using Stride.Core.Storage;
using Stride.Engine;
using Stride.Graphics.Font;
using Stride.Graphics.Font.RuntimeMsdf;
using Xunit;

namespace Stride.Graphics.Tests;

public class RuntimeMsdfRasterizerAndExtractorRegressionTests
{
    [Fact]
    public void OutlineDiagnosticRasterizer_Renders_AsymmetricShape_WithExpectedOrientation()
    {
        var outline = CreateAsymmetricLShapeOutline();
        var settings = new DistanceFieldSettings(PixelRange: 4, Padding: 2, Width: 20, Height: 20);

        using var bitmap = ((IGlyphMsdfRasterizer)new OutlineDiagnosticRasterizer())
            .RasterizeMsdf(outline, settings, MsdfEncodeSettings.Default);

        Assert.Equal(settings.TotalWidth, bitmap.Width);
        Assert.Equal(settings.TotalHeight, bitmap.Rows);

        var scan = ScanBitmap(bitmap);
        Assert.True(scan.NonBlackCount > 0, "Expected rasterized diagnostic outline pixels.");

        int topQuarterRows = Math.Max(1, bitmap.Rows / 4);
        int bottomQuarterStart = bitmap.Rows - topQuarterRows;
        int topCount = CountNonBlackPixels(bitmap, yStart: 0, yEndExclusive: topQuarterRows);
        int bottomCount = CountNonBlackPixels(bitmap, yStart: bottomQuarterStart, yEndExclusive: bitmap.Rows);

        Assert.True(topCount > bottomCount,
            $"Expected asymmetric shape to have more signal in top rows than bottom rows (top={topCount}, bottom={bottomCount}).");
    }

    [Fact]
    public void MsdfGenCoreRasterizer_RasterizeMsdf_RectangleOutline_ProducesNonEmptyBitmap()
    {
        var outline = CreateRectangleOutline(0, 0, 8, 6);
        var settings = new DistanceFieldSettings(PixelRange: 4, Padding: 3, Width: 24, Height: 18);

        using var bitmap = ((IGlyphMsdfRasterizer)new MsdfGenCoreRasterizer())
            .RasterizeMsdf(outline, settings, MsdfEncodeSettings.Default);

        Assert.Equal(settings.TotalWidth, bitmap.Width);
        Assert.Equal(settings.TotalHeight, bitmap.Rows);

        var scan = ScanBitmap(bitmap);
        Assert.True(scan.NonBlackCount > 0, "Expected non-empty MSDF output.");
        Assert.True(scan.MinRgb != scan.MaxRgb, "Expected non-uniform MSDF values in output bitmap.");
    }

    [Fact]
    public void FreeTypeOutlineExtractor_ExtractsSimpleGlyph_WithNonEmptyContoursAndBounds()
    {
        Game.InitializeAssetDatabase();

        using var fontManager = new FontManager(CreateDatabaseProvider());

        bool extracted = fontManager.TryGetGlyphOutline(
            fontFamily: "Arial",
            fontStyle: FontStyle.Regular,
            size: new Vector2(64, 64),
            character: 'A',
            outline: out var outline,
            metrics: out var metrics);

        Assert.True(extracted);
        Assert.NotNull(outline);
        Assert.NotEmpty(outline.Contours);

        int segmentCount = outline.Contours.Sum(c => c?.Segments?.Count ?? 0);
        Assert.True(segmentCount > 0, "Expected at least one extracted outline segment.");

        Assert.True(outline.Bounds.Width > 0);
        Assert.True(outline.Bounds.Height > 0);

        Assert.True(metrics.AdvanceX > 0);
        Assert.True(metrics.Width > 0);
        Assert.True(metrics.Height > 0);
        Assert.True(float.IsFinite(metrics.BearingX));
        Assert.True(float.IsFinite(metrics.BearingY));
    }

    private static IDatabaseFileProviderService CreateDatabaseProvider()
    {
        Stride.Core.IO.VirtualFileSystem.CreateDirectory(Stride.Core.IO.VirtualFileSystem.ApplicationDatabasePath);
        return new DatabaseFileProviderService(new DatabaseFileProvider(ObjectDatabase.CreateDefaultDatabase()));
    }

    private static GlyphOutline CreateAsymmetricLShapeOutline()
    {
        var contour = new GlyphContour();
        var p0 = new Vector2(0, 0);
        var p1 = new Vector2(0, 4);
        var p2 = new Vector2(4, 4);
        var p3 = new Vector2(4, 3);
        var p4 = new Vector2(1, 3);
        var p5 = new Vector2(1, 0);

        contour.Segments.Add(new LineSegment(p0, p1));
        contour.Segments.Add(new LineSegment(p1, p2));
        contour.Segments.Add(new LineSegment(p2, p3));
        contour.Segments.Add(new LineSegment(p3, p4));
        contour.Segments.Add(new LineSegment(p4, p5));
        contour.Segments.Add(new LineSegment(p5, p0));

        var outline = new GlyphOutline
        {
            Bounds = new RectangleF(0, 0, 4, 4),
            Winding = GlyphWinding.Clockwise,
        };
        outline.Contours.Add(contour);
        return outline;
    }

    private static GlyphOutline CreateRectangleOutline(float x, float y, float width, float height)
    {
        var p0 = new Vector2(x, y);
        var p1 = new Vector2(x + width, y);
        var p2 = new Vector2(x + width, y + height);
        var p3 = new Vector2(x, y + height);

        var contour = new GlyphContour();
        contour.Segments.Add(new LineSegment(p0, p1));
        contour.Segments.Add(new LineSegment(p1, p2));
        contour.Segments.Add(new LineSegment(p2, p3));
        contour.Segments.Add(new LineSegment(p3, p0));

        var outline = new GlyphOutline { Bounds = new RectangleF(x, y, width, height) };
        outline.Contours.Add(contour);
        return outline;
    }

    private static (int NonBlackCount, int MinRgb, int MaxRgb) ScanBitmap(CharacterBitmapRgba bitmap)
    {
        var bytes = CopyBitmapBytes(bitmap);
        int nonBlackCount = 0;
        int minRgb = 255;
        int maxRgb = 0;

        for (int i = 0; i < bytes.Length; i += 4)
        {
            int r = bytes[i];
            int g = bytes[i + 1];
            int b = bytes[i + 2];

            if (r != 0 || g != 0 || b != 0)
                nonBlackCount++;

            minRgb = Math.Min(minRgb, Math.Min(r, Math.Min(g, b)));
            maxRgb = Math.Max(maxRgb, Math.Max(r, Math.Max(g, b)));
        }

        return (nonBlackCount, minRgb, maxRgb);
    }

    private static int CountNonBlackPixels(CharacterBitmapRgba bitmap, int yStart, int yEndExclusive)
    {
        var bytes = CopyBitmapBytes(bitmap);
        int count = 0;

        for (int y = yStart; y < yEndExclusive; y++)
        {
            int rowStart = y * bitmap.Pitch;
            for (int x = 0; x < bitmap.Width; x++)
            {
                int offset = rowStart + x * 4;
                if (bytes[offset] != 0 || bytes[offset + 1] != 0 || bytes[offset + 2] != 0)
                    count++;
            }
        }

        return count;
    }

    private static byte[] CopyBitmapBytes(CharacterBitmapRgba bitmap)
    {
        int size = checked(bitmap.Pitch * bitmap.Rows);
        var bytes = new byte[size];
        if (size > 0)
            Marshal.Copy(bitmap.Buffer, bytes, 0, size);
        return bytes;
    }
}
