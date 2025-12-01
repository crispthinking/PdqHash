using FFMpegCore;

namespace PdqHash.Hashing.Video;

public static class VideoExtensions
{
    extension(VPdqHasher hasher)
    {
        public async Task<double> DetermineFramerateAsync(Uri uri, long? desiredFrameCount = null, CancellationToken cancellationToken = default) =>
            DetermineFrameRate(desiredFrameCount, await FFProbe.AnalyseAsync(uri, null, cancellationToken));

        public async Task<double> DetermineFramerateAsync(string filePath, long? desiredFrameCount = null, CancellationToken cancellationToken = default) =>
            DetermineFrameRate(desiredFrameCount, await FFProbe.AnalyseAsync(filePath, null, cancellationToken));
    }

    private static double DetermineFrameRate(long? desiredFrameCount, IMediaAnalysis analysis)
    {
        if (analysis.PrimaryVideoStream != null)
        {
            var totalSeconds = analysis.Duration.TotalSeconds;

            if (desiredFrameCount.HasValue)
            {
                return Math.Round(desiredFrameCount.Value / totalSeconds, 2, MidpointRounding.AwayFromZero);
            }

            return totalSeconds switch
            {
                < 5 => 10, // 10 fps for videos <= 5 secs?
                < 60 => 2, // 2 fps for videos <= 1min?
                < 300 => 1, // 1 secs per frame for videos <= 5 mins?
                < 600 => 0.5, // 2 secs per frame for videos <= 10 mins?
                _ => 0.25, // 4 secs per frame for videos > 10 mins?
            };
        }
        else
        {
            throw new InvalidOperationException("Unable to determine framerate automatically as the primary video stream was undetected.");
        }
    }
}