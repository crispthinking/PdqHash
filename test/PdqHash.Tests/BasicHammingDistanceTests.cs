using PdqHash;

namespace PdqHashing.Tests;

public class BasicHammingDistanceTests
{
    const string SAMPLE_HASH = "9c151c3af838278e3ef57c180c7d031c07aefd12f2ccc1e18f2a1e1c7d0ff163";
    const string SAMPLE_HASH_PLUS = "9d151c3af838278e3ef57c180c7d031c07aefd12f2ccc1e18f2a1e1c7d0ff163";

    [Fact]
    public void TestHammingDistance()
    {
        var hash2 = new PdqHash256();

        var hash1 = PdqHash256.fromHexString(SAMPLE_HASH);
        hash2.Clear();

        Assert.Equal(128, hash1.hammingDistance(hash2));
    }

    [Fact]
    public void TestHammingDistance2()
    {
        var hash1 = PdqHash256.fromHexString(SAMPLE_HASH);
        var hash2 = PdqHash256.fromHexString(SAMPLE_HASH_PLUS);
        hash2.Clear();

        Assert.Equal(128, hash1.hammingDistance(hash2));
    }
}
