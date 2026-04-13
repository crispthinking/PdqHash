using System.Drawing;
using System.Globalization;
using System.Runtime.CompilerServices;
using FFMpegCore;
using FFMpegCore.Enums;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace PdqHash;

public static class FFMegImageExt
{
    extension(Uri input)
    {
        public async IAsyncEnumerable<(Image<Rgba32> Screenshot, int Frame, TimeSpan? TotalDuration)> GetSnapshotsFromUri(System.Drawing.Size? size = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var source = FFProbe.Analyse(input);
            var baseDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            try
            {
                await Task.Run(() =>
                    FFMpegArguments
                        .FromUrlInput(input)
                        .OutputToFile(baseDir + "Frame%05d.png", true, opts => opts
                            .WithVideoCodec("png")
                            .WithFramerate(0.5)
                            .Resize(size))
                        .ProcessAsynchronously(true), cancellationToken);

                foreach (var file in Directory.EnumerateFiles(baseDir))
                {
                    var index = int.Parse(Path.GetFileNameWithoutExtension(file)[5..], CultureInfo.InvariantCulture);

                    var bitmap = SixLabors.ImageSharp.Image.Load<Rgba32>(file);
                    yield return (bitmap, index, source.PrimaryVideoStream?.Duration);
                    bitmap.Dispose();
                }
            }
            finally
            {
                foreach (var file in Directory.EnumerateFiles(baseDir))
                {
                    File.Delete(file);
                }
            }
        }
    }
}