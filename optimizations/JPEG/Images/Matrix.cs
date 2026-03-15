using System;
using System.Buffers;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading.Tasks;

namespace JPEG.Images;

public class Matrix : IDisposable
{
    public readonly int Height;
    public readonly int Width;

    public readonly float[] Y;
    public readonly float[] Cb;
    public readonly float[] Cr;

    public Matrix(int height, int width)
    {
        Height = height;
        Width = width;

        Y = ArrayPool<float>.Shared.Rent(height * width);
        Cb = ArrayPool<float>.Shared.Rent(height * width);
        Cr = ArrayPool<float>.Shared.Rent(height * width);
    }

    public static explicit operator Matrix(Bitmap bmp)
    {
        var height = bmp.Height - bmp.Height % 8;
        var width = bmp.Width - bmp.Width % 8;

        var matrix = new Matrix(height, width);

        var rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
        var bmpData = bmp.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);

        var stride = bmpData.Stride;

        unsafe
        {
            var ptr = (byte*)bmpData.Scan0;
            Parallel.For(0, height, j =>
            {
                var start = ptr + j * stride;
                var rowOffset = j * width;

                for (var i = 0; i < width; i++)
                {
                    var curr = start + i * 3;
                    var index = rowOffset + i;

                    var b = curr[0];
                    var g = curr[1];
                    var r = curr[2];

                    var _y = 16.0f + (65.738f * r + 129.057f * g + 24.064f * b) / 256.0f;
                    var cb = 128.0f + (-37.945f * r - 74.494f * g + 112.439f * b) / 256.0f;
                    var cr = 128.0f + (112.439f * r - 94.154f * g - 18.285f * b) / 256.0f;

                    matrix.Y[index] = _y;
                    matrix.Cb[index] = cb;
                    matrix.Cr[index] = cr;
                }
            });
        }

        bmp.UnlockBits(bmpData);
        return matrix;
    }

    public static explicit operator Bitmap(Matrix matrix)
    {
        var bmp = new Bitmap(matrix.Width, matrix.Height, PixelFormat.Format24bppRgb);

        var rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
        var bmpData = bmp.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);

        var matrixHeight = matrix.Height;
        var matrixWidth = matrix.Width;
        var stride = bmpData.Stride;

        unsafe
        {
            var ptr = (byte*)bmpData.Scan0;
            Parallel.For(0, matrixHeight, j =>
            {
                var start = ptr + j * stride;
                var rowOffset = j * matrixWidth;

                for (var i = 0; i < matrixWidth; i++)
                {
                    var curr = start + i * 3;
                    var index = rowOffset + i;

                    var _y = matrix.Y[index];
                    var cb = matrix.Cb[index];
                    var cr = matrix.Cr[index];

                    var r = (298.082f * _y + 408.583f * cr) / 256.0f - 222.921f;
                    var g = (298.082f * _y - 100.291f * cb - 208.120f * cr) / 256.0f + 135.576f;
                    var b = (298.082f * _y + 516.412f * cb) / 256.0f - 276.836f;

                    curr[0] = ToByte(b);
                    curr[1] = ToByte(g);
                    curr[2] = ToByte(r);
                }
            });
        }

        bmp.UnlockBits(bmpData);
        return bmp;
    }

    public void Dispose()
    {
        ArrayPool<float>.Shared.Return(Y);
        ArrayPool<float>.Shared.Return(Cb);
        ArrayPool<float>.Shared.Return(Cr);
    }

    private static byte ToByte(float d)
    {
        var val = (int)Math.Round(d);
        if (val > byte.MaxValue)
            return byte.MaxValue;
        if (val < byte.MinValue)
            return byte.MinValue;
        return (byte)val;
    }
}