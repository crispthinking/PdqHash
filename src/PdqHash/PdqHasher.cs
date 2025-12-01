using System.Buffers;
using System.Diagnostics;
using CommunityToolkit.HighPerformance;
using CommunityToolkit.HighPerformance.Buffers;
using SkiaSharp;

namespace PdqHash.Hashing;

public class PdqHasher : IDisposable
{
    const float LUMA_FROM_R_COEFF = 0.299F;
    const float LUMA_FROM_G_COEFF = 0.587F;
    const float LUMA_FROM_B_COEFF = 0.114F;

    //  From Wikipedia: standard RGB to luminance (the 'Y' in 'YUV').
    static readonly double DCT_MATRIX_SCALE_FACTOR = Math.Sqrt(2.0 / 64.0);
    private readonly IMemoryOwner<float> _dctMatrixOwner;
    private readonly Memory<float> _dctMemory;

    //  Wojciech Jarosz 'Fast Image Convolutions' ACM SIGGRAPH 2001:
    //  X,Y,X,Y passes of 1-D box filters produces a 2D tent filter.
    const int PDQ_NUM_JAROSZ_XY_PASSES = 2;

    // #  Since PDQ uses 64x64 blocks, 1/64th of the image height/width
    // #  respectively is a full block. But since we use two passes, we want half
    // #  that window size per pass. Example: 1024x1024 full-resolution input. PDQ
    // #  downsamples to 64x64. Each 16x16 block of the input produces a single
    // #  downsample pixel.  X,Y passes with window size 8 (= 1024/128) average
    // #  pixels with 8x8 neighbors. The second X,Y pair of 1D box-filter passes
    // #  accumulate data from all 16x16.
    const int PDQ_JAROSZ_WINDOW_SIZE_DIVISOR = 128;

    public PdqHasher()
    {
        _dctMatrixOwner = MemoryPool<float>.Shared.Rent(16 * 64);
        _dctMemory = _dctMatrixOwner.Memory.Slice(0, 16 * 64);
        ComputeDCTMatrix(_dctMemory);
    }

    public Span2D<float> DCTMatrix => _dctMemory.Span.AsSpan2D(16, 64);

    private static void ComputeDCTMatrix(Memory<float> memory)
    {
        var dctMatrix = memory.Span.AsSpan2D(16, 64);

        for (var i = 0; i < 16; i++)
        {
            for (var j = 0; j < 64; j++)
            {
                dctMatrix[i, j] = (float)(DCT_MATRIX_SCALE_FACTOR * Math.Cos(Math.PI / 2 / 64 * (i + 1) * (2 * j + 1)));
            }
        }
    }

    public HashResult? FromFile(string filePath)
    {
        if (File.Exists(filePath) is false)
        {
            throw new ArgumentException($"FilePath: {filePath} does not exist");
        }

        using var input = File.OpenRead(filePath);
        return FromStream(input, filePath);
    }

    public HashResult? FromStream(Stream input, string source)
    {
        var stopwatch = Stopwatch.StartNew();
        
        using var codec = SKCodec.Create(input, out var result);

        if (codec == null)
        {
            throw new ArgumentException($"Failed to parse codec from SKImage stream. Reason: {result}");
        }

        using var original = SKBitmap.Decode(codec);

        if (original == null)
        {
            // https://github.com/mono/SkiaSharp/issues/2429
            throw new ArgumentException($"Failed to parse input stream as a valid SKImage stream. Ensure the stream is able to read minimum of {SKCodec.MinBufferedBytesNeeded} to parse Codec info.");
        }

        var width = Math.Min(original.Width, 1024);
        var height = Math.Min(original.Height, 1024);

        using var resized = original.Resize(new SKImageInfo(width, height)
        {
            ColorSpace = SKColorSpace.CreateSrgb(),
        }, SKSamplingOptions.Default);

        var readSeconds = stopwatch.Elapsed.TotalSeconds;
        var numCols = resized.Width;
        var numRows = resized.Height;

        using var buffer1 = SpanOwner<float>.Allocate(resized.Height * resized.Width);
        using var buffer2 = SpanOwner<float>.Allocate(resized.Height * resized.Width);

        using var buffer64x64Owner = SpanOwner<float>.Allocate(64 * 64);
        using var buffer16x16Owner = SpanOwner<float>.Allocate(16 * 16);

        var buffer64x64 = buffer64x64Owner.Span.AsSpan2D(64, 64);
        var buffer16x16 = buffer64x64Owner.Span.AsSpan2D(16, 16);

        PdqStatistics.ReadDuration.Record(stopwatch.ElapsedMilliseconds);
        stopwatch.Restart();

        var rv = FromImage(resized, buffer1.Span, buffer2.Span, buffer64x64, buffer16x16);

        PdqStatistics.HashDuration.Record(stopwatch.ElapsedMilliseconds);
        PdqStatistics.HashesGenerated.Add(1);

        return new HashResult(rv.Hash, rv.Quality,
            new HashingStatistics(readSeconds, stopwatch.Elapsed.TotalSeconds, numRows * numCols, source));
    }


    public HashResult FromBitmap(SKBitmap resized, string source)
    {
        var stopwatch = Stopwatch.StartNew();

        var readSeconds = stopwatch.Elapsed.TotalSeconds;
        var numCols = resized.Width;
        var numRows = resized.Height;

        using var buffer1 = SpanOwner<float>.Allocate(resized.Height * resized.Width);
        using var buffer2 = SpanOwner<float>.Allocate(resized.Height * resized.Width);

        using var buffer64x64Owner = SpanOwner<float>.Allocate(64 * 64);
        using var buffer16x16Owner = SpanOwner<float>.Allocate(16 * 16);

        var buffer64x64 = buffer64x64Owner.Span.AsSpan2D(64, 64);
        var buffer16x16 = buffer64x64Owner.Span.AsSpan2D(16, 16);

        PdqStatistics.ReadDuration.Record(stopwatch.ElapsedMilliseconds);
        stopwatch.Restart();

        var rv = FromImage(resized, buffer1.Span, buffer2.Span, buffer64x64, buffer16x16);

        PdqStatistics.HashDuration.Record(stopwatch.ElapsedMilliseconds);
        PdqStatistics.HashesGenerated.Add(1);

        return new HashResult(rv.Hash, rv.Quality,
            new HashingStatistics(readSeconds, stopwatch.Elapsed.TotalSeconds, numRows * numCols, source));
    }


    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing) 
    {
        if (disposing)
        {
            _dctMatrixOwner?.Dispose();
        }
    }

    private HashAndQuality FromImage(SKBitmap img, Span<float> buffer1, Span<float> buffer2, Span2D<float> buffer64x64, Span2D<float> buffer16x16)
    {
        var numCols = img.Width;
        var numRows = img.Height;
        FillFloatLumaFromBufferImage(img, buffer1);
        return PdqHash256FromFloatLuma(buffer1, buffer2, numRows, numCols, buffer64x64, buffer16x16);
    }

    private HashAndQuality PdqHash256FromFloatLuma(Span<float> buffer1, Span<float> buffer2, int numRows, int numCols, Span2D<float> buffer64x64, Span2D<float> buffer16x16)
    {
        var windowSizeAlongRows = ComputeJaroszFilterWindowSize(numCols);
        var windowSizeAlongCols = ComputeJaroszFilterWindowSize(numRows);

        JaroszFilterFloat(
            buffer1,
            buffer2,
            numRows,
            numCols,
            windowSizeAlongRows,
            windowSizeAlongCols,
            PDQ_NUM_JAROSZ_XY_PASSES
        );

        DecimateFloat(buffer1, numRows, numCols, buffer64x64);
        var quality = ComputePDQImageDomainQualityMetric(buffer64x64);

        Dct64To16(buffer64x64, buffer16x16);
        var hash = PdqBuffer16x16ToBits(buffer16x16);
        return new HashAndQuality(hash, quality);
    }

    /// <summary>
    /// Each bit of the 16x16 output hash is for whether the given frequency
    /// component is greater than the median frequency component or not.
    /// </summary>
    /// <param name="dctOutput16x16"></param>
    /// <returns></returns>
    private static PdqHash256 PdqBuffer16x16ToBits(Span2D<float> dctOutput16x16)
    {
        var hash = new PdqHash256();
        var dctMedian = dctOutput16x16.Torben(16, 16);

        for (var i = 0; i < 16; i++)
        {
            for (var j = 0; j < 16; j++)
            {
                if (dctOutput16x16[i, j] > dctMedian)
                {
                    hash.setBit(i * 16 + j);
                }
            }
        }
        return hash;
    }

    /// <summary>
    /// Full 64x64 to 64x64 can be optimized e.g. the Lee algorithm.
    /// But here we only want slots (1-16)x(1-16) of the full 64x64 output.
    /// Careful experiments showed that using Lee along all 64 slots in one
    /// dimension, then Lee along 16 slots in the second, followed by
    /// extracting slots 1-16 of the output, was actually slower than the
    /// current implementation which is completely non-clever/non-Lee but
    /// computes only what is needed.
    /// </summary>
    private void Dct64To16(Span2D<float> A, Span2D<float> B)
    {
        using var TOwner = SpanOwner<float>.Allocate(16 * 64);
        var T = TOwner.Span.AsSpan2D(16, 64);

        for (var i = 0; i < 16; i++)
        {
            for (var j = 0; j < 64; j++)
            {
                var tij = 0.0F;
                for (var k = 0; k < 64; k++)
                {
                    tij += DCTMatrix[i, k] * A[k, j];
                }
                T[i, j] = tij;
            }
        }

        for (var i = 0; i < 16; i++)
        {
            for (var j = 0; j < 16; j++)
            {
                var sumk = 0.0F;
                for (var k = 0; k < 64; k++)
                {
                    sumk += T[i, k] * DCTMatrix[j, k];
                }
                B[i, j] = sumk;
            }
        }
    }

    /// <summary>
    /// This is all heuristic (see the PDQ hashing doc). Quantization
    /// matters since we want to count *significant* gradients, not just the
    /// some of many small ones. The constants are all manually selected, and
    /// tuned as described in the document.
    /// </summary>
    private static int ComputePDQImageDomainQualityMetric(Span2D<float> buffer64x64)
    {
        var gradientSum = 0;
        for (var i = 0; i < 63; i++)
        {
            for (var j = 0; j < 64; j++)
            {
                var u = buffer64x64[i, j];
                var v = buffer64x64[i + 1, j];
                var d = (int)((u - v) * 100 / 255);
                gradientSum += Math.Abs(d);
            }
        }
        for (var i = 0; i < 64; i++)
        {
            for (var j = 0; j < 63; j++)
            {
                var u = buffer64x64[i, j];
                var v = buffer64x64[i, j + 1];
                var d = (int)((u - v) * 100 / 255);
                gradientSum += Math.Abs(d);
            }
        }
        var quality = gradientSum / 90;
        if (quality > 100)
            quality = 100;
        return quality;
    }

    private static void DecimateFloat(Span<float> in_, int inNumRows, int inNumCols, Span2D<float> output)
    {
        for (var i = 0; i < 64; i++)
        {
            var ini = (int)((i + 0.5) * inNumRows / 64);
            for (var j = 0; j < 64; j++)
            {
                var inj = (int)((j + 0.5) * inNumCols / 64);
                output[i, j] = in_[ini * inNumCols + inj];
            }
        }
    }

    private static void JaroszFilterFloat(Span<float> buffer1, Span<float> buffer2, int numRows, int numCols, int windowSizeAlongRows, int windowSizeAlongCols, int nreps)
    {
        for (var _i = 0; _i < nreps; _i++)
        {
            BoxAlongRowsFloat(buffer1, buffer2, numRows, numCols, windowSizeAlongRows);
            BoxAlongColsFloat(buffer2, buffer1, numRows, numCols, windowSizeAlongCols);
        }
    }

    /// <param name="input">matrix as numRows x numCols in row-major order</param>
    /// <param name="output">matrix as numRows x numCols in row-major order</param>
    /// <param name="numRows"></param>
    /// <param name="numCols"></param>
    /// <param name="windowSize"></param>
    private static void BoxAlongColsFloat(Span<float> input, Span<float> output, int numRows, int numCols, int windowSize)
    {
        var j = 0;
        while (j < numCols)
        {
            Box1DFloat(input, j, output, j, numRows, numCols, windowSize);
            j++;
        }
    }

    /// <param name="input">matrix as numRows x numCols in row-major order</param>
    /// <param name="output">matrix as numRows x numCols in row-major order</param>
    /// <param name="numRows"></param>
    /// <param name="numCols"></param>
    /// <param name="windowSize"></param>
    private static void BoxAlongRowsFloat(Span<float> input, Span<float> output, int numRows, int numCols, int windowSize)
    {
        var i = 0;
        while (i < numRows)
        {
            Box1DFloat(input,
            i * numCols,
            output,
            i * numCols,
            numCols,
            1,  // stride
            windowSize);
            i++;
        }
    }

    private static void Box1DFloat(Span<float> invec, int inStartOffset, Span<float> outVec, int outStartOffset, int vectorLength, int stride, int fullWindowSize)
    {
        var halfWindowSize = (int)((fullWindowSize + 2) / 2);
        var phase_1_nreps = (int)(halfWindowSize - 1);
        var phase_2_nreps = (int)(fullWindowSize - halfWindowSize + 1);
        var phase_3_nreps = (int)(vectorLength - fullWindowSize);
        var phase_4_nreps = (int)(halfWindowSize - 1);
        var li = 0;  // Index of left edge of read window, for subtracts
        var ri = 0;  // Index of right edge of read windows, for adds
        var oi = 0;  // Index into output vector
        var sum = 0.0F;
        var currentWindowSize = 0;

        var i = 0;
        while (i < phase_1_nreps)
        {
            sum += invec[inStartOffset + ri];
            currentWindowSize++;
            ri += stride;
            i++;
        }

        // PHASE 2: INITIAL WRITES WITH SMALL WINDOW
        i = 0;
        while (i < phase_2_nreps)
        {
            sum += invec[inStartOffset + ri];
            currentWindowSize += 1;
            outVec[outStartOffset + oi] = sum / currentWindowSize;
            ri += stride;
            oi += stride;
            i++;
        }

        //PHASE 3: WRITES WITH FULL WINDOW
        i = 0;
        while (i < phase_3_nreps)
        {
            sum += invec[inStartOffset + ri];
            sum -= invec[inStartOffset + li];
            outVec[outStartOffset + oi] = sum / currentWindowSize;
            li += stride;
            ri += stride;
            oi += stride;
            i++;
        }
        // PHASE 4: FINAL WRITES WITH SMALL WINDOW
        i = 0;
        while (i < phase_4_nreps)
        {
            sum -= invec[inStartOffset + li];
            currentWindowSize -= 1;
            outVec[outStartOffset + oi] = sum / currentWindowSize;
            li += stride;
            oi += stride;
            i += 1;
        }
    }

    private static int ComputeJaroszFilterWindowSize(int dimensionSize)
    {
        return (dimensionSize + PDQ_JAROSZ_WINDOW_SIZE_DIVISOR - 1) / PDQ_JAROSZ_WINDOW_SIZE_DIVISOR;
    }

    private static void FillFloatLumaFromBufferImage(SKBitmap img, Span<float> luma)
    {
        var numCols = img.Width;
        var numRows = img.Height;

        if (img.ColorSpace != null && img.ColorSpace.IsSrgb is false)
        {
            throw new InvalidOperationException("Failed to transform image to sRGB color space");
        }

        for (var row = 0; row < numRows; row++)
        {
            for (var col = 0; col < numCols; col++)
            {
                var pixel = img.GetPixel(col, row);
                
                luma[row * numCols + col] = (
                    LUMA_FROM_R_COEFF * pixel.Red +
                    LUMA_FROM_G_COEFF * pixel.Green +
                    LUMA_FROM_B_COEFF * pixel.Blue
                );
            }
        }

    }
}
