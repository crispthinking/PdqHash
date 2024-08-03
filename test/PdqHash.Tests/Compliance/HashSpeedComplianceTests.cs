using System.Diagnostics;
using PdqHash;
using PdqHash.Hashing;

namespace PdqHashing.Tests.Compliance;

public class HashSpeedComplianceTests
{
    private const string BASE_DIR = "../../../../../assets/DISC21/";

    [Fact]
    public void EnsureHashesPerSecondKept()
    {
        var hasher = new PdqHasher();
        var files = Directory.EnumerateFiles(BASE_DIR).ToList();
        var _ = hasher.FromFile(files.First());
        var iterations = 20;
        var stopwatch = Stopwatch.StartNew();

        for (int i = 0; i < iterations; i++)
        {
            foreach (var item in files)
            {
                hasher.FromFile(item);
            }   
        }

        stopwatch.Stop();

        Assert.InRange(stopwatch.ElapsedMilliseconds / ((double)files.Count * iterations), 0, 20);
    }
}
