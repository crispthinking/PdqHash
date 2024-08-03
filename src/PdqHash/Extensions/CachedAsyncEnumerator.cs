
namespace PdqHash.Hashing.Extensions;

public class CachedAsyncEnumerator<T> : IAsyncEnumerator<T>
{
    private readonly IEnumerator<T> _staticEnumerator;
    private bool _staticEnumerationComplete;
    private readonly List<T> _items;
    private readonly IAsyncEnumerator<T> _asyncEnumerator;
    private T? _current;

    public CachedAsyncEnumerator(List<T> backingList, IAsyncEnumerator<T> enumerator)
    {
        _items = backingList;
        _staticEnumerator = _items.GetEnumerator();
        _staticEnumerationComplete = false;
        _asyncEnumerator = enumerator;
        _current = default;
    }

    public T Current => _current ?? throw new InvalidOperationException("Cannot access current item without iteration");

    public async ValueTask<bool> MoveNextAsync()
    {
        if (_staticEnumerationComplete == false)
        {
            if (_staticEnumerator.MoveNext())
            {
                _current = _staticEnumerator.Current;
                return true;
            }
            else
            {
                _staticEnumerationComplete = true;
                _staticEnumerator.Dispose();
            }
        }

        if (await _asyncEnumerator.MoveNextAsync())
        {
            _items.Add(_asyncEnumerator.Current);
            _current = _asyncEnumerator.Current;
            return true;
        }
        else
        {
            _current = default;
            return false;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeAsync(true);
        GC.SuppressFinalize(this);
    }

    protected virtual ValueTask DisposeAsync(bool disposing)
    {
        if (disposing)
        {
            _staticEnumerator.Dispose();
            return _asyncEnumerator.DisposeAsync();
        }

        return ValueTask.CompletedTask;
    }
}
