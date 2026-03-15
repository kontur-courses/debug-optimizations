using System;

namespace JPEG;

public static class DCT
{
    private const int Size = 8;
    private const int BlockSize = Size * Size;
    private const float Beta = 0.25f;

    private static readonly float[] AlphaCos = new float[BlockSize];

    static DCT()
    {
        for (var u = 0; u < Size; u++)
        {
            var alpha = u == 0 ? (float)(1.0 / Math.Sqrt(2.0)) : 1.0f;
            for (var x = 0; x < Size; x++)
            {
                AlphaCos[u * Size + x] =
                    alpha * (float)Math.Cos(((2.0 * x + 1.0) * u * Math.PI) / 16.0);
            }
        }
    }

    public static void DCT2D(Span<float> data)
    {
        Span<float> coeffs = stackalloc float[BlockSize];
        DCT2D(data, coeffs);
        coeffs.CopyTo(data);
    }

    public static void DCT2D(ReadOnlySpan<float> input, Span<float> coeffs)
    {
        Span<float> tmp = stackalloc float[BlockSize];

        for (var y = 0; y < Size; y++)
        {
            var rowOffset = y * Size;
            for (var u = 0; u < Size; u++)
            {
                var acOffset = u * Size;
                var sum = 0.0f;
                for (var x = 0; x < Size; x++)
                {
                    sum += input[rowOffset + x] * AlphaCos[acOffset + x];
                }

                tmp[rowOffset + u] = sum;
            }
        }

        for (var v = 0; v < Size; v++)
        {
            var acOffset = v * Size;
            for (var u = 0; u < Size; u++)
            {
                var sum = 0.0f;
                for (var y = 0; y < Size; y++)
                {
                    sum += tmp[y * Size + u] * AlphaCos[acOffset + y];
                }

                coeffs[v * Size + u] = sum * Beta;
            }
        }
    }

    public static void IDCT2D(Span<float> data)
    {
        Span<float> output = stackalloc float[BlockSize];
        IDCT2D(data, output);
        output.CopyTo(data);
    }

    public static void IDCT2D(ReadOnlySpan<float> coeffs, Span<float> output)
    {
        Span<float> tmp = stackalloc float[BlockSize];

        for (var v = 0; v < Size; v++)
        {
            var rowOffset = v * Size;
            for (var x = 0; x < Size; x++)
            {
                var sum = 0.0f;
                for (var u = 0; u < Size; u++)
                {
                    sum += coeffs[rowOffset + u] * AlphaCos[u * Size + x];
                }

                tmp[rowOffset + x] = sum;
            }
        }

        for (var y = 0; y < Size; y++)
        {
            for (var x = 0; x < Size; x++)
            {
                var sum = 0.0f;
                for (var v = 0; v < Size; v++)
                {
                    sum += tmp[v * Size + x] * AlphaCos[v * Size + y];
                }

                output[y * Size + x] = sum * Beta;
            }
        }
    }
}