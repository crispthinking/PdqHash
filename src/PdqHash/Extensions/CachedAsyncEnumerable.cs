#nullable enable

namespace PdqHash.Hashing.Extensions;

public class CachedAsyncEnumerable<T> : IAsyncEnumerable<T?>
{
    private readonly IAsyncEnumerator<T> _asyncEnumerator;
    private readonly List<T> _materialized;

    public CachedAsyncEnumerable(IAsyncEnumerable<T> asyncEnumerable)
    {
        _asyncEnumerator = asyncEnumerable.GetAsyncEnumerator();
        _materialized = new List<T>();
    }

    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) =>
        new CachedAsyncEnumerator<T>(_materialized, _asyncEnumerator);
}