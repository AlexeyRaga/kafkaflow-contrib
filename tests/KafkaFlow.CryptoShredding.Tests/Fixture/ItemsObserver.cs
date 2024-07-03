using System.Collections.Concurrent;

namespace KafkaFlow.CryptoShredding.Tests.Fixture;

public sealed class ItemsObserver<T> : IDisposable
{
    private readonly ConcurrentDictionary<Subscription<T>, bool> _subscriptions = [];

    public Subscription<T> Subscribe(Func<T, bool> subscribeUntil)
    {
        var subscription = new Subscription<T>(subscribeUntil, x => _subscriptions.TryRemove(x, out _));
        _subscriptions.TryAdd(subscription, true);
        return subscription;
    }

    public void Observe(T item)
    {
        foreach (var subscription in _subscriptions.Keys)
        {
            subscription.Evaluate(item);
        }
    }

    public void Dispose()
    {
        foreach (var subscription in _subscriptions.Keys)
        {
            subscription.Dispose();
        }
    }
}

public sealed class Subscription<T>(Func<T, bool> predicate, Action<Subscription<T>> unsubscribe) : IDisposable
{
    private readonly TaskCompletionSource<bool> _tcs = new();
    private readonly object _lock = new();
    private readonly List<T> _items = [];

    public IReadOnlyList<T> Items
    {
        get { return _items.AsReadOnly(); }
    }

    public void Evaluate(T item)
    {
        lock (_lock)
        {
            if (_tcs.Task.IsCompleted) return;
            if (predicate(item))
            {
                _items.Add(item);
                _tcs.TrySetResult(true);
            }
            else
            {
                _items.Add(item);
            }
        }
    }

    public async Task AwaitFor(TimeSpan timeout)
    {
        using var cts = new CancellationTokenSource(timeout);
        var completedTask = await Task.WhenAny(_tcs.Task, Task.Delay(timeout, cts.Token));
        try
        {
            if (completedTask == _tcs.Task)
            {
                await _tcs.Task;
            }
            else
            {
                lock (_lock)
                {
                    if (_tcs.TrySetCanceled(cts.Token))
                    {
                        throw new TimeoutException($"Operation timed out after {timeout}");
                    }
                }
            }
        }
        finally
        {
            Dispose(); // Unsubscribe upon completion or timeout
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            _tcs.TrySetCanceled();
        }

        unsubscribe(this);
    }
}
