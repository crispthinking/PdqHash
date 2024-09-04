using System.Diagnostics;
using PdqHash.Hashing;
using Xunit.Abstractions;

namespace PdqHashing.Tests.Compliance;

public class HashSpeedComplianceTests(ITestOutputHelper output)
{
    private const string BASE_DIR = "../../../../../assets/DISC21/";

    [Fact]
    public void EnsureHashesPerSecondKept()
    {
        var hasher = new PdqHasher();
        var files = Directory.EnumerateFiles(BASE_DIR, "*.jpg").ToList();
        var _ = hasher.FromFile(files.First());
        var iterations = 50;
        var stopwatch = Stopwatch.StartNew();

        for (int i = 0; i < iterations; i++)
        {
            foreach (var item in files)
            {
                hasher.FromFile(item);
            }   
        }
        
        stopwatch.Stop();
        output.WriteLine($"Processed {files.Count * iterations} hashes in {stopwatch.Elapsed.TotalSeconds} seconds, hash rate: {stopwatch.ElapsedMilliseconds / ((double)files.Count * iterations)} p/s");

        var hashRateRounded = (int)Math.Floor(stopwatch.ElapsedMilliseconds / (double)files.Count * iterations);

        Assert.InRange(hashRateRounded, 25, int.MaxValue);
    }
}
