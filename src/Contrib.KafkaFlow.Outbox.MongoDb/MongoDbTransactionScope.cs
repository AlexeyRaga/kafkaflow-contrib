using KafkaFlow.Outbox;
using MongoDB.Driver;

namespace Contrib.KafkaFlow.Outbox.MongoDb;

internal sealed class MongoDbTransactionScope : ITransactionScope
{
    private readonly IClientSessionHandle? _session;
    private readonly bool _supportsTransactions;
    private readonly bool _ownsSession; // Do we own this session or are we reusing an existing one?
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private bool _completed;
    private bool _disposed;

    // AsyncLocal to make session available to repository operations
    private static readonly AsyncLocal<IClientSessionHandle?> CurrentSessionRef = new();

    public static IClientSessionHandle? CurrentSession => CurrentSessionRef.Value;

    // Private constructor - use Create method instead
    private MongoDbTransactionScope(IClientSessionHandle? session, bool supportsTransactions, bool ownsSession)
    {
        _session = session;
        _supportsTransactions = supportsTransactions;
        _ownsSession = ownsSession;
    }

    /// <summary>
    /// Creates a new transaction scope. Currently behaves like "Required" -
    /// reuses existing session if available, creates new one if not.
    /// </summary>
    public static MongoDbTransactionScope Create(IMongoClient client)
    {
        // Check if there's already an active session we can reuse
        if (CurrentSessionRef.Value != null)
        {
            // Reuse existing session - we don't own it
            return new MongoDbTransactionScope(
                session: CurrentSessionRef.Value,
                supportsTransactions: true, // Assume it supports transactions if it exists
                ownsSession: false);
        }

        // No existing session, create a new one
        try
        {
            var session = client.StartSession();
            var supportsTransactions = true;

            try
            {
                session.StartTransaction();
            }
            catch (NotSupportedException)
            {
                // Standalone MongoDB servers don't support transactions
                // but we can still use the session for consistency
                supportsTransactions = false;
            }

            // Make session available to repository operations
            CurrentSessionRef.Value = session;

            return new MongoDbTransactionScope(
                session: session,
                supportsTransactions: supportsTransactions,
                ownsSession: true);
        }
        catch (MongoException)
        {
            // Other MongoDB exceptions (e.g., can't create session at all)
            CurrentSessionRef.Value = null;
            return new MongoDbTransactionScope(
                session: null,
                supportsTransactions: false,
                ownsSession: false);
        }
    }

    public void Complete()
    {
        _semaphore.Wait();
        try
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(MongoDbTransactionScope));

            // Only commit if we own the session and haven't completed yet
            if (!_completed && _ownsSession && _supportsTransactions && _session != null)
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
                // Only abort and cleanup if we own the session
                if (_ownsSession && !_completed && _supportsTransactions && _session != null)
                    _session.AbortTransaction();
            }
            finally
            {
                // Only clear AsyncLocal and dispose session if we own it
                if (_ownsSession)
                {
                    CurrentSessionRef.Value = null;
                    _session?.Dispose();
                }
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
