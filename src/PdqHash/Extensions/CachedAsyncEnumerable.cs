#nullable enable

namespace PdqHash.Hashing.Extensions;

public class CachedAsyncEnumerable<T>(IAsyncEnumerable<T> asyncEnumerable) : IAsyncEnumerable<T?>
{
    private readonly IAsyncEnumerator<T> _asyncEnumerator = asyncEnumerable.GetAsyncEnumerator();
    private readonly List<T> _materialized = [];

    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) =>
        new CachedAsyncEnumerator<T>(_materialized, _asyncEnumerator);

    /// <summary>
    /// Returns the number of elements that have been materialized so far.
    /// This is only accurate after the enumerable has been fully consumed.
    /// </summary>
    public int Count => _materialized.Count;
}