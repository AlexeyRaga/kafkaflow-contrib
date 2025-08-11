using KafkaFlow.Outbox;
using MongoDB.Driver;

namespace Contrib.KafkaFlow.Outbox.MongoDb;

internal sealed class MongoDbTransactionScope : ITransactionScope
{
    private readonly IClientSessionHandle? _session;
    private readonly bool _supportsTransactions;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private bool _completed;
    private bool _disposed;

    public MongoDbTransactionScope(IMongoClient client)
    {
        try
        {
            _session = client.StartSession();
            _session.StartTransaction();
            _supportsTransactions = true;
        }
        catch (NotSupportedException)
        {
            // Standalone MongoDB servers don't support transactions
            _session = null;
            _supportsTransactions = false;
        }
        catch (MongoException)
        {
            // Other MongoDB exceptions (e.g., replica set not configured)
            _session = null;
            _supportsTransactions = false;
        }
    }

    public void Complete()
    {
        _semaphore.Wait();
        try
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(MongoDbTransactionScope));

            if (!_completed && _supportsTransactions && _session != null)
            {
                _session.CommitTransaction();
                _completed = true;
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public void Dispose()
    {
        _semaphore.Wait();
        try
        {
            if (_disposed)
                return;

            try
            {
                if (!_completed && _supportsTransactions && _session != null)
                    _session.AbortTransaction();
            }
            finally
            {
                _session?.Dispose();
                _disposed = true;
            }
        }
        finally
        {
            _semaphore.Release();
            _semaphore.Dispose();
        }
    }
}
