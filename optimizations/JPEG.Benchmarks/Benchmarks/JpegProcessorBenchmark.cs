using System;
using System.Drawing;
using BenchmarkDotNet.Attributes;
using JPEG.Images;
using JPEG.Processor;

namespace JPEG.Benchmarks.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(warmupCount: 2, iterationCount: 3)]
public class JpegProcessorBenchmark
{
    private IJpegProcessor jpegProcessor = null!;
    private static readonly string imagePath = @"sample.bmp";
    private static readonly string compressedImagePath = imagePath + ".compressed." + JpegProcessor.CompressionQuality;
    private static readonly string uncompressedImagePath = 
        imagePath + ".uncompressed." + JpegProcessor.CompressionQuality + ".bmp";
    
    private Bitmap bmp = null!;
    private Matrix matrix = null!;
    private const int Size = 64;
    private float[] dctData = null!;

    [GlobalSetup]
    public void SetUp()
    {
        var random = new Random(100);
        dctData = new float[Size];
        for (var i = 0; i < Size; i++)
        {
            dctData[i] = random.Next(-128, 128);
        }

        jpegProcessor = JpegProcessor.Init;
        bmp = new Bitmap(imagePath);
        matrix = (Matrix)bmp;
        jpegProcessor.Compress(imagePath, compressedImagePath);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        matrix.Dispose();
        bmp.Dispose();
    }

    [Benchmark]
    public void Compress()
    {
        jpegProcessor.Compress(imagePath, compressedImagePath);
    }

    [Benchmark]
    public void Uncompress()
    {
        jpegProcessor.Uncompress(compressedImagePath, uncompressedImagePath);
    }

    [Benchmark]
    public void CompressAndUncompress()
    {
        jpegProcessor.Compress(imagePath, compressedImagePath);
        jpegProcessor.Uncompress(compressedImagePath, uncompressedImagePath);
    }

    [Benchmark]
    public void DCT2D()
    {
        Span<float> dctCopy = stackalloc float[Size];
        dctData.AsSpan().CopyTo(dctCopy);

        DCT.DCT2D(dctCopy);
    }

    [Benchmark]
    public void IDCT2D()
    {
        Span<float> idctCopy = stackalloc float[Size];
        dctData.AsSpan().CopyTo(idctCopy);

        DCT.IDCT2D(idctCopy);
    }

    [Benchmark]
    public void DCT2DAndIDCT2D()
    {
        Span<float> block = stackalloc float[Size];
        dctData.AsSpan().CopyTo(block);

        DCT.DCT2D(block);
        DCT.IDCT2D(block);
    }

    [Benchmark]
    public void BitmapToMatrix()
    {
        using var result = (Matrix)bmp;
    }

    [Benchmark]
    public void MatrixToBitmap()
    {
        using var result = (Bitmap)matrix;
    }

    [Benchmark]
    public void BitmapToMatrixToBitmap()
    {
        using var matrixResult = (Matrix)bmp;
        using var bitmapResult = (Bitmap)matrixResult;
    }

    [Benchmark]
    public void MatrixToBitmapToMatrix()
    {
        using var bitmapResult = (Bitmap)matrix;
        using var matrixResult = (Matrix)bitmapResult;
    }
}