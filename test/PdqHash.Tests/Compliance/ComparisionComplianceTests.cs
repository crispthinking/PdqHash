using System.Diagnostics;
using PdqHash;
using PdqHash.Hashing;
using Snapshooter.Xunit;

namespace PdqHashing.Tests.Compliance;

public class ComparisionComplianceTests
{
    private const int MAX_REFERENCE_DISTANCE = 0;
    private const string BASE_DIR = "../../../../../assets/DISC21/";

    public static IEnumerable<object[]> Data =>
        Directory.EnumerateFiles(
            Path.GetFullPath(BASE_DIR), "*.jpg")
        .Select(path => new object[] { Path.GetFileName(path) });

    [Theory]
    [InlineData("Q25683.jpg", "Q22447.jpg", 14)]
    [InlineData("Q25683.jpg", "Q48489.jpg", 20)]
    [InlineData("Q25683.jpg", "Q29945.jpg", 20)]
    [InlineData("Q25683.jpg", "Q46299.jpg", 38)]
        public void TestHammingDistance(string fileName, string queryFile, int expectedDistance)
    {
        var fullPath = Path.Combine(BASE_DIR, fileName);
        var queryFullPath = Path.Combine(BASE_DIR, queryFile);

        var hasher = new PdqHasher();
        var result = hasher.FromFile(fullPath);

        Assert.NotNull(result);
        Assert.InRange(result.Quality, 49, 100);

        var sourceHash = result.Hash.toHexString();
        
        var queryResult = hasher.FromFile(queryFullPath);
        Assert.NotNull(queryResult);
        Assert.InRange(queryResult.Quality, 49, 100);

        var distance = result.Hash.hammingDistance(queryResult.Hash);

        Assert.Equal(expectedDistance, distance);
    }
}
