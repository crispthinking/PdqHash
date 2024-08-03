using System.Diagnostics.Metrics;

namespace PdqHash.Hashing;

public sealed class PdqStatistics
{
    public static Meter Meter { get; } = new Meter("Pdq");

    public static Counter<int> HashesGenerated { get; } = Meter.CreateCounter<int>("pdq.hash.generated", "hashes", "Number of hashes generated");
    public static Histogram<double> HashDuration { get; } = Meter.CreateHistogram<double>("pdq.hash.duration", "ms", "Avg duration taken to generate a hash");
    public static Histogram<double> ReadDuration { get; } = Meter.CreateHistogram<double>("pdq.io.read-duration", "ms", "Avg duration taken to read hash input bytes");
}