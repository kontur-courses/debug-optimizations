using System;
using System.Buffers;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
using JPEG.Images;

namespace JPEG.Processor;

public class JpegProcessor : IJpegProcessor
{
    public static readonly JpegProcessor Init = new();
    public const int CompressionQuality = 70;
    private const int DCTSize = 8;
    private const int TotalSize = DCTSize * DCTSize;

    private static readonly int[] ZigZagMap = new[]
    {
        0, 1, 8, 16, 9, 2, 3, 10,
        17, 24, 32, 25, 18, 11, 4, 5,
        12, 19, 26, 33, 40, 48, 41, 34,
        27, 20, 13, 6, 7, 14, 21, 28,
        35, 42, 49, 56, 57, 50, 43, 36,
        29, 22, 15, 23, 30, 37, 44, 51,
        58, 59, 52, 45, 38, 31, 39, 46,
        53, 60, 61, 54, 47, 55, 62, 63
    };

    private static readonly int[] QuantizationMatrix = new[]
    {
        16, 11, 10, 16, 24, 40, 51, 61,
        12, 12, 14, 19, 26, 58, 60, 55,
        14, 13, 16, 24, 40, 57, 69, 56,
        14, 17, 22, 29, 51, 87, 80, 62,
        18, 22, 37, 56, 68, 109, 103, 77,
        24, 35, 55, 64, 81, 104, 113, 92,
        49, 64, 78, 87, 103, 121, 120, 101,
        72, 92, 95, 98, 112, 100, 103, 99
    };

    public void Compress(string imagePath, string compressedImagePath)
    {
        using var fileStream = File.OpenRead(imagePath);
        using var bmp = (Bitmap)Image.FromStream(fileStream, false, false);
        using var imageMatrix = (Matrix)bmp;
        var compressionResult = Compress(imageMatrix, CompressionQuality);
        compressionResult.Save(compressedImagePath);
    }

    public void Uncompress(string compressedImagePath, string uncompressedImagePath)
    {
        var compressedImage = CompressedImage.Load(compressedImagePath);
        using var uncompressedImage = Uncompress(compressedImage);
        using var resultBmp = (Bitmap)uncompressedImage;
        resultBmp.Save(uncompressedImagePath, ImageFormat.Bmp);
    }

    private static CompressedImage Compress(Matrix matrix, int quality = 50)
    {
        var height = matrix.Height;
        var width = matrix.Width;
        var maxSize = height * width * 3;
        var blocksPerRow = width / DCTSize;
        var blockPerHeight = height / DCTSize;
        var allQuantizedBytes = ArrayPool<byte>.Shared.Rent(maxSize);

        try
        {
            var quantizationMatrix = GetQuantizationMatrix(quality);
            Parallel.For(0, blockPerHeight, blockRow =>
            {
                EncodeBlockRow(matrix, blockRow, blocksPerRow, quantizationMatrix, allQuantizedBytes);
            });

            var data = new ArraySegment<byte>(allQuantizedBytes, 0, maxSize);
            long bitsCount;
            Dictionary<BitsWithLength, byte> decodeTable;
            var compressedBytes = HuffmanCodec.Encode(data, out decodeTable, out bitsCount);

            return new CompressedImage
            {
                Quality = quality,
                CompressedBytes = compressedBytes,
                BitsCount = bitsCount,
                DecodeTable = decodeTable,
                Height = matrix.Height,
                Width = matrix.Width
            };
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(allQuantizedBytes);
        }
    }

    private static void EncodeBlockRow(
        Matrix matrix,
        int blockRow,
        int blocksPerRow,
        int[] quantizationMatrix,
        byte[] output)
    {
        var y = blockRow * DCTSize;
        Span<float> subMatrix = stackalloc float[TotalSize];
        Span<byte> quantizedFreqs = stackalloc byte[TotalSize];
        Span<byte> quantizedBytes = stackalloc byte[TotalSize];

        for (var blockColumn = 0; blockColumn < blocksPerRow; blockColumn++)
        {
            var x = blockColumn * DCTSize;
            var outputOffset = (blockRow * blocksPerRow + blockColumn) * 3 * TotalSize;

            EncodeChannel(matrix, y, x, matrix.Y, quantizationMatrix, subMatrix, quantizedFreqs, quantizedBytes, output, outputOffset);
            EncodeChannel(matrix, y, x, matrix.Cb, quantizationMatrix, subMatrix, quantizedFreqs, quantizedBytes, output, outputOffset + TotalSize);
            EncodeChannel(matrix, y, x, matrix.Cr, quantizationMatrix, subMatrix, quantizedFreqs, quantizedBytes, output, outputOffset + 2 * TotalSize);
        }
    }

    private static void EncodeChannel(
        Matrix matrix,
        int yOffset,
        int xOffset,
        float[] channel,
        int[] quantizationMatrix,
        Span<float> subMatrix,
        Span<byte> quantizedFreqs,
        Span<byte> quantizedBytes,
        byte[] output,
        int outputOffset)
    {
        GetSubMatrix(matrix, yOffset, xOffset, channel, subMatrix);
        ShiftMatrixValues(subMatrix, -128);
        DCT.DCT2D(subMatrix);
        Quantize(subMatrix, quantizationMatrix, quantizedFreqs);
        ZigZagScan(quantizedFreqs, quantizedBytes);
        quantizedBytes.CopyTo(output.AsSpan(outputOffset, TotalSize));
    }

    private static Matrix Uncompress(CompressedImage image)
    {
        var result = new Matrix(image.Height, image.Width);
        var quantizationMatrix = GetQuantizationMatrix(image.Quality);
        var blocksPerRow = image.Width / DCTSize;
        var blockPerHeight = image.Height / DCTSize;

        var decodedData = HuffmanCodec.Decode(
            image.CompressedBytes,
            image.DecodeTable,
            image.BitsCount,
            image.Height * image.Width * 3);

        Parallel.For(0, blockPerHeight, blockRow =>
        {
            DecodeBlockRow(decodedData, result, blockRow, blocksPerRow, quantizationMatrix);
        });

        return result;
    }

    private static void DecodeBlockRow(
        byte[] source,
        Matrix result,
        int blockRow,
        int blocksPerRow,
        int[] quantizationMatrix)
    {
        var y = blockRow * DCTSize;
        Span<float> _y = stackalloc float[TotalSize];
        Span<float> cb = stackalloc float[TotalSize];
        Span<float> cr = stackalloc float[TotalSize];
        Span<byte> quantizedFreqs = stackalloc byte[TotalSize];

        for (var blockColumn = 0; blockColumn < blocksPerRow; blockColumn++)
        {
            var x = blockColumn * DCTSize;
            var sourceOffset = (blockRow * blocksPerRow + blockColumn) * 3 * TotalSize;

            sourceOffset = DecodeChannel(source, sourceOffset, quantizationMatrix, _y, quantizedFreqs);
            sourceOffset = DecodeChannel(source, sourceOffset, quantizationMatrix, cb, quantizedFreqs);
            DecodeChannel(source, sourceOffset, quantizationMatrix, cr, quantizedFreqs);

            SetPixels(result, _y, cb, cr, y, x);
        }
    }

    private static int DecodeChannel(
        ReadOnlySpan<byte> source,
        int sourceOffset,
        int[] quantizationMatrix,
        Span<float> channelData,
        Span<byte> quantizedFreqs)
    {
        var quantizedBytes = source.Slice(sourceOffset, TotalSize);

        ZigZagUnScan(quantizedBytes, quantizedFreqs);
        DeQuantize(quantizedFreqs, quantizationMatrix, channelData);
        DCT.IDCT2D(channelData);
        ShiftMatrixValues(channelData, 128);

        return sourceOffset + TotalSize;
    }

    private static void ShiftMatrixValues(Span<float> subMatrix, int shiftValue)
    {
        for (var i = 0; i < TotalSize; i++)
        {
            subMatrix[i] += shiftValue;
        }
    }

    private static void SetPixels(Matrix matrix, Span<float> _y, Span<float> cb, Span<float> cr, int yOffset, int xOffset)
    {
        for (var y = 0; y < DCTSize; y++)
        {
            for (var x = 0; x < DCTSize; x++)
            {
                var imageIndex = (yOffset + y) * matrix.Width + xOffset + x;
                var fragmentIndex = y * DCTSize + x;
                matrix.Y[imageIndex] = _y[fragmentIndex];
                matrix.Cb[imageIndex] = cb[fragmentIndex];
                matrix.Cr[imageIndex] = cr[fragmentIndex];
            }
        }
    }

    private static void GetSubMatrix(Matrix matrix, int yOffset, int xOffset, float[] channel, Span<float> result)
    {
        var width = matrix.Width;
        for (var j = 0; j < DCTSize; j++)
        {
            for (var i = 0; i < DCTSize; i++)
            {
                var imageIndex = (yOffset + j) * width + xOffset + i;
                var fragmentIndex = j * DCTSize + i;
                result[fragmentIndex] = channel[imageIndex];
            }
        }
    }

    private static void ZigZagScan(ReadOnlySpan<byte> channelFreqs, Span<byte> result)
    {
        for (var i = 0; i < TotalSize; i++)
        {
            result[i] = channelFreqs[ZigZagMap[i]];
        }
    }

    private static void ZigZagUnScan(ReadOnlySpan<byte> quantizedBytes, Span<byte> result)
    {
        for (var i = 0; i < TotalSize; i++)
        {
            result[ZigZagMap[i]] = quantizedBytes[i];
        }
    }

    private static void Quantize(ReadOnlySpan<float> channelFreqs, int[] quantizationMatrix, Span<byte> result)
    {
        for (var i = 0; i < TotalSize; i++)
        {
            result[i] = (byte)(channelFreqs[i] / quantizationMatrix[i]);
        }
    }

    private static void DeQuantize(ReadOnlySpan<byte> quantizedBytes, int[] quantizationMatrix, Span<float> result)
    {
        for (var i = 0; i < TotalSize; i++)
        {
            result[i] = ((sbyte)quantizedBytes[i]) * quantizationMatrix[i];
        }
    }

    private static int[] GetQuantizationMatrix(int quality)
    {
        if (quality < 1 || quality > 99)
            throw new ArgumentException("quality must be in [1,99] interval");

        var multiplier = quality < 50 ? 5000 / quality : 200 - 2 * quality;
        var quantizationMatrix = new int[TotalSize];
        for (var i = 0; i < TotalSize; i++)
        {
            var quantized = (multiplier * QuantizationMatrix[i] + 50) / 100;
            quantizationMatrix[i] = Math.Max(1, quantized);
        }

        return quantizationMatrix;
    }
}