using System.IO.Pipelines;
using PdqHash.Hashing;

namespace PdqHashing.Tests.Compliance;

public class StreamParsingTests()
{
    private const string BASE_DIR = "../../../../../assets/DISC21/";

    [Fact]
    public void EnsureHashingWithSeekableStreams()
    {
        var hasher = new PdqHasher();
                 
        var pipe = new Pipe();

        var stream = File.Open(Path.Combine(BASE_DIR, "R050000.jpg"), FileMode.Open, FileAccess.Read);

        var hashTask = Task.Run(() =>
        {
            return hasher.FromStream(pipe.Reader.AsStream(), "");
        });

        var hash = hasher.FromStream(stream, "");
        
        Assert.NotNull(hash);
    }

    [Fact]
    public async Task EnsureHashingWithNonSeekableStreams()
    {
        var hasher = new PdqHasher();
                 
        var pipe = new Pipe();

        var bytes = File.ReadAllBytes(Path.Combine(BASE_DIR, "R050001.jpg"));

        var hashTask = Task.Run(() =>
        {
            return hasher.FromStream(pipe.Reader.AsStream(), "");
        });

        var writeTask = Task.Run(() =>pipe.Writer.WriteAsync(bytes));

        await Task.WhenAll(hashTask, writeTask);

        var hash = await hashTask; 
        
        Assert.NotNull(hash);
    }
}