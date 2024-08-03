using System.Diagnostics;
using PdqHash;
using PdqHash.Hashing;

namespace PdqHashing.Tests.Compliance;

public class ThreatExchangeComplianceTests
{
    private const int MAX_REFERENCE_DISTANCE = 14;
    private const string BASE_DIR = "../../../../../assets/DISC21/";

    public static IEnumerable<object[]> Data =>
        Directory.EnumerateFiles(
            Path.GetFullPath(BASE_DIR), "*.jpg")
        .Select(path => new object[] { Path.GetFileName(path) });

    [Theory]
    [MemberData(nameof(Data))]
    public async Task TestHammingDistance(string fileName)
    {
        var fullPath = Path.Combine(BASE_DIR, fileName);

        var hasher = new PdqHasher();
        var result = hasher.FromFile(fullPath);
        
        Assert.NotNull(result);
        Assert.InRange(result.Quality, 49, 100);

        var info = new ProcessStartInfo()
        {
            FileName = "/Users/james.abbott/.local/bin/threatexchange",
            Arguments = $"hash photo {fullPath}",
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using var process = Process.Start(info);
        Assert.NotNull(process);

        var output = await process.StandardOutput.ReadToEndAsync();
        Assert.StartsWith("pdq ", output);

        var pdq = output["pdq ".Length..].TrimStart().TrimEnd();
        var referenceHash = PdqHash256.fromHexString(pdq);
        
        var distance = result.Hash.hammingDistance(referenceHash);        

        Assert.InRange(distance, 0, MAX_REFERENCE_DISTANCE);
    }
}
