
namespace PdqHash.Hashing.Extensions;

public class CachedAsyncEnumerator<T>(List<T> backingList, IAsyncEnumerator<T> enumerator) : IAsyncEnumerator<T>
{
    private readonly IEnumerator<T> _staticEnumerator = backingList.GetEnumerator();
    private bool _staticEnumerationComplete;
    private readonly List<T> _items = backingList;
    private readonly IAsyncEnumerator<T> _asyncEnumerator = enumerator;
    
    public T Current { get => field ?? throw new InvalidOperationException("Cannot access current item without iteration"); private set; }

    public async ValueTask<bool> MoveNextAsync()
    {
        if (_staticEnumerationComplete is false)
        {
            if (_staticEnumerator.MoveNext())
            {
                Current = _staticEnumerator.Current;
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
            Current = _asyncEnumerator.Current;
            return true;
        }
        else
        {
            Current = default!;
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
