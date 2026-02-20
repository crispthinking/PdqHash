using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Runtime.CompilerServices;
using FFMpegCore;
using FFMpegCore.Enums;

namespace PdqHash.Hashing.Video;

public class VPdqHasher : IDisposable
{
    private readonly PdqHasher _hasher;

    public VPdqHasher()
    {
        _hasher = new PdqHasher();
    }

    private bool HashVideoFrameFile(
        int index,
        FileInfo file,
        PdqHash256? lastHash,
        out PdqHash256 hash,
        out int quality,
        out int distance)
    {
        try
        {
            var result = _hasher.FromFile(file.FullName);

            hash = result?.Hash ?? throw new InvalidOperationException($"Failed to create hash from file: {file.FullName}");
            quality = result.Quality;
            distance = 0;

            if (quality < 49)
            {
                Trace.WriteLine($"Frame {index} has poor quality {quality}, skipping");
                return false;
            }

            if (lastHash != null)
            {
                distance = hash.hammingDistance(lastHash.Value);
                if (distance < 30)
                {
                    Trace.WriteLine($"Frame {index} has distance {distance} from last frame, skipping");
                    return false;
                }
            }

            return true;
        }
        finally
        {
            file.Delete();
        }
    }


    private async IAsyncEnumerable<VPdqHash> HashVideoUsingFfmpegAsync(FFMpegArgumentProcessor ffmpegArgs, DirectoryInfo baseDir, double frameRate, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        PdqHash256? lastHash = null;
        var ffmpegComplete = false;
        using var ffmpegCompletion = new SemaphoreSlim(0);

        try
        {
            var ffmpegTask = Task.Run(async () =>
            {
                await ffmpegArgs.ProcessAsynchronously(true);
                ffmpegCompletion.Release();
            }, cancellationToken);

            var orderedFiles = baseDir
                .EnumerateFiles()
                .Select(f => (f, index: int.Parse(Path.GetFileNameWithoutExtension(f.FullName)[5..], CultureInfo.InvariantCulture)))
                .OrderBy(fi => fi.index);

            while (
                ffmpegComplete is false || orderedFiles.Any())
            {
                if (ffmpegTask.IsFaulted)
                {
                    await ffmpegTask;
                }

                foreach (var (file, index) in orderedFiles)
                {
                    if (HashVideoFrameFile(index, file, lastHash, out var hash, out var quality, out var distance))
                    {
                        yield return new VPdqHash
                        {
                            Distance = distance,
                            Hash = hash,
                            Frame = index,
                            Timestamp = TimeSpan.FromSeconds(index / frameRate)
                        };

                        lastHash = hash;
                    }
                }

                orderedFiles = baseDir
                    .EnumerateFiles()
                    .Select(f => (f, index: int.Parse(Path.GetFileNameWithoutExtension(f.FullName)[5..], CultureInfo.InvariantCulture)))
                    .OrderBy(fi => fi.index);

                ffmpegComplete = ffmpegComplete || await ffmpegCompletion.WaitAsync(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }
        finally
        {
            foreach (var file in baseDir.EnumerateFiles())
            {
                File.Delete(file.FullName);
            }
            baseDir.Delete();
        }
    }

    public IAsyncEnumerable<VPdqHash> FromUriAsync(Uri uri, double frameRate, CancellationToken cancellationToken = default)
    {
        var baseDir = Directory.CreateTempSubdirectory("PdqNet");
        var args = FFMpegArguments
            .FromUrlInput(uri)
            .OutputToFile(baseDir + "/Frame%05d.png", true, opts => opts
                .WithVideoCodec("png")
                .WithFramerate(frameRate)
                .Resize(new Size(512, 512)));
        return HashVideoUsingFfmpegAsync(args, baseDir, frameRate, cancellationToken);
    }

    public IAsyncEnumerable<VPdqHash> FromFileAsync(string fileName, double frameRate, CancellationToken cancellationToken = default)
    {
        var baseDir = Directory.CreateTempSubdirectory("PdqNet");
        var args = FFMpegArguments
                    .FromFileInput(fileName)
                    .OutputToFile(baseDir + "/Frame%05d.png", true, opts => opts
                        .WithVideoCodec("png")
                        .WithFramerate(frameRate)
                        .Resize(new Size(512, 512)));

        return HashVideoUsingFfmpegAsync(args, baseDir, frameRate, cancellationToken);
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
            _hasher?.Dispose();
        }
    }
}