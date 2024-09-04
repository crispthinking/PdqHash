using System.IO.Pipelines;
using PdqHash.Hashing;

namespace PdqHashing.Tests.Streams;

public class StreamParsingTests()
{
    private const string BASE_DIR = "../../../Streams/";

    [Fact]
    public void EnsureHashingWithSeekableStreams()
    {
        var hasher = new PdqHasher();
                 
        var pipe = new Pipe();

        var stream = File.Open(Path.Combine(BASE_DIR, "EnsureHashingWithSeekableStreams.jpg"), FileMode.Open, FileAccess.Read);

        var hashTask = Task.Run(() =>
        {
            return hasher.FromStream(pipe.Reader.AsStream(), "");
        });

        var hash = hasher.FromStream(stream, "");
        
        Assert.NotNull(hash);
    }

    [Fact]
    public void EnsureHashingWithUnSeekableInvalidStream()
    {
        var hasher = new PdqHasher();
                 
        var pipe = new Pipe();

        var stream = File.Open(Path.Combine(BASE_DIR, "InvalidImage.txt"), FileMode.Open, FileAccess.Read);

        var hashTask = Task.Run(() =>
        {
            return hasher.FromStream(pipe.Reader.AsStream(), "");
        });

        Assert.ThrowsAny<ArgumentException>(() =>
        {
            var hash = hasher.FromStream(stream, "");        
            Assert.Null(hash);
        });
    }


    [Fact]
    public async Task EnsureHashingWithNonSeekableStreams()
    {
        var hasher = new PdqHasher();
                 
        var pipe = new Pipe();

        var bytes = File.ReadAllBytes(Path.Combine(BASE_DIR, "EnsureHashingWithNonSeekableStreams.jpg"));

        var hashTask = Task.Run(() =>
        {
            return hasher.FromStream(pipe.Reader.AsStream(), "");
        });

        var writeTask = Task.Run(() => pipe.Writer.WriteAsync(bytes));

        await Task.WhenAll(hashTask, writeTask);

        var hash = await hashTask; 
        
        Assert.NotNull(hash);
    }
}