using System.Buffers;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Text;

namespace PdqHash.Hashing.Video;

public static class VPdqParser
{
    public static async IAsyncEnumerable<VPdqHash> FromStreamAsync(Stream stream, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var pipe = new Pipe();
        var reading = Task.Run(() => stream.CopyToAsync(pipe.Writer, cancellationToken), cancellationToken);
        var writing = FromPipeReaderAsync(pipe.Reader, cancellationToken);

        await foreach (var hash in writing)
        {
            yield return hash;
        }

        await reading;
    }

    public static IEnumerable<VPdqHash> FromString(string value)
    {
        using var reader = new StringReader(value);

        var line = reader.ReadLine();
        if (!string.IsNullOrEmpty(line))
        {
            var bytes = Encoding.UTF8.GetBytes(line);
            yield return VPdqHash.FromBytes(bytes);
        }
    }

    public static async IAsyncEnumerable<VPdqHash> FromPipeReaderAsync(PipeReader reader, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            ReadResult result = await reader.ReadAsync(cancellationToken);
            ReadOnlySequence<byte> buffer = result.Buffer;

            while (TryReadLine(ref buffer, out ReadOnlySequence<byte> line))
            {
                // Process the line.
                var hash = VPdqHash.FromBytes(line.ToArray());
                yield return hash;
            }

            // Tell the PipeReader how much of the buffer has been consumed.
            reader.AdvanceTo(buffer.Start, buffer.End);

            // Stop reading if there's no more data coming.
            if (result.IsCompleted)
            {
                break;
            }
        }

        // Mark the PipeReader as complete.
        await reader.CompleteAsync();
    }

    static bool TryReadLine(ref ReadOnlySequence<byte> buffer, out ReadOnlySequence<byte> line)
    {
        // Look for a EOL in the buffer.
        SequencePosition? position = buffer.PositionOf((byte)'\n');

        if (position == null)
        {
            line = default;
            return false;
        }

        // Skip the line + the \n.
        line = buffer.Slice(0, position.Value);
        buffer = buffer.Slice(buffer.GetPosition(1, position.Value));
        return true;
    }

}