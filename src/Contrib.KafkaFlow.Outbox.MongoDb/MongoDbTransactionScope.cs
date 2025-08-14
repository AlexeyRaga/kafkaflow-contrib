using System.Diagnostics.CodeAnalysis;
using KafkaFlow.Outbox;
using MongoDB.Driver;

namespace Contrib.KafkaFlow.Outbox.MongoDb;

/// <summary>
/// <para>
/// Represents a transaction scope for MongoDB operations with session ownership management.
/// </para>
/// <para>
/// This class manages the lifecycle of a MongoDB session, allowing for transaction management
/// and ensuring that operations can be committed or rolled back. It uses an AsyncLocal to make
/// the session available to repository operations throughout the current execution context.
/// </para>
/// </summary>
/// <remarks>
/// <para>
/// The scope implements a "Required" transaction pattern that supports nested scopes:
/// </para>
/// <list type="bullet">
/// <item><description>If an existing session is already active, it reuses that session without taking ownership</description></item>
/// <item><description>If no session exists, it creates a new session and takes ownership of it</description></item>
/// </list>
/// <para>
/// <strong>Session Ownership:</strong> Only scopes that own their session will manage transaction lifecycle.
/// Nested scopes that reuse existing sessions will not commit or abort transactions.
/// </para>
/// <para>
/// <strong>Transaction Support:</strong> The scope automatically detects if the MongoDB server supports
/// transactions. If transactions are not supported (e.g., standalone servers), operations will still
/// work but without transactional guarantees.
/// </para>
/// <para>
/// <strong>Transaction Lifecycle:</strong> For owned sessions only:
/// </para>
/// <list type="bullet">
/// <item><description>If <c>Complete()</c> is called before disposal, the transaction will be committed</description></item>
/// <item><description>If <c>Complete()</c> is not called, the transaction will be aborted upon disposal</description></item>
/// <item><description>Non-owned sessions (nested scopes) are not affected by <c>Complete()</c> or disposal</description></item>
/// </list>
/// </remarks>
public sealed class MongoDbTransactionScope : ITransactionScope
{
    private readonly IClientSessionHandle? _session;
    private readonly bool _supportsTransactions;
    private readonly bool _ownsSession; // Do we own this session or are we reusing an existing one?
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private bool _completed;
    private bool _disposed;

    // AsyncLocal to make session available to repository operations
    private static readonly AsyncLocal<IClientSessionHandle?> CurrentSessionRef = new();

    /// <summary>
    /// Gets a value indicating whether there is an active MongoDB session in the current execution context.
    /// </summary>
    /// <value>
    /// <c>true</c> if a session is available; otherwise, <c>false</c>.
    /// </value>
    /// <remarks>
    /// This property can be used to check if MongoDB operations will have access to a session
    /// without needing to access the session itself.
    /// </remarks>
    public static bool HasSession => CurrentSessionRef.Value != null;

    /// <summary>
    /// Gets the current MongoDB session available in the execution context, if any.
    /// </summary>
    /// <value>
    /// The current <see cref="IClientSessionHandle"/> if available; otherwise, <c>null</c>.
    /// </value>
    /// <remarks>
    /// <para>
    /// This property provides access to the MongoDB session that can be used by repository operations
    /// to ensure they participate in the current transaction scope.
    /// </para>
    /// <para>
    /// The session is managed through AsyncLocal, making it available throughout the current
    /// execution context including async operations.
    /// </para>
    /// </remarks>
    public static IClientSessionHandle? CurrentSession => CurrentSessionRef.Value;

    /// <summary>
    /// Gets a value indicating whether the current scope supports MongoDB transactions.
    /// </summary>
    /// <value>
    /// <c>true</c> if the scope can perform transactional operations; otherwise, <c>false</c>.
    /// </value>
    /// <remarks>
    /// <para>
    /// This property indicates whether the MongoDB server and configuration support transactions.
    /// It will be <c>false</c> for standalone MongoDB servers or when session creation fails.
    /// </para>
    /// <para>
    /// Even when transactions are not supported, the scope can still provide session consistency
    /// for read operations.
    /// </para>
    /// </remarks>
    public bool SupportsTransactions => _supportsTransactions;

    /// <summary>
    /// Attempts to get the current MongoDB session from the execution context.
    /// </summary>
    /// <param name="session">
    /// When this method returns, contains the current session if available; otherwise, <c>null</c>.
    /// </param>
    /// <returns>
    /// <c>true</c> if a session is available in the current execution context; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method provides a safe way to check for and retrieve the current session without
    /// needing to handle null reference scenarios when accessing <see cref="CurrentSession"/> directly.
    /// </para>
    /// <para>
    /// This is particularly useful in repository implementations that need to conditionally
    /// use a session if available, or operate without one if not.
    /// </para>
    /// </remarks>
    public static bool TryGetSession([NotNullWhen(true)] out IClientSessionHandle? session)
    {
        session = CurrentSessionRef.Value;
        return session != null;
    }

    // Private constructor - use Create method instead
    private MongoDbTransactionScope(IClientSessionHandle? session, bool supportsTransactions, bool ownsSession)
    {
        _session = session;
        _supportsTransactions = supportsTransactions;
        _ownsSession = ownsSession;
    }

    /// <summary>
    /// Creates a new MongoDB transaction scope using the "Required" transaction pattern.
    /// </summary>
    /// <param name="client">The MongoDB client to use for creating a new session if needed.</param>
    /// <returns>
    /// A new <see cref="MongoDbTransactionScope"/> that either owns a new session or
    /// reuses an existing session without ownership.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method implements the "Required" pattern:
    /// </para>
    /// <list type="bullet">
    /// <item><description>If a session already exists in the current context, it will be reused without taking ownership</description></item>
    /// <item><description>If no session exists, a new session will be created and owned by this scope</description></item>
    /// </list>
    /// <para>
    /// Only scopes that own their session will manage transaction commit/abort operations.
    /// </para>
    /// </remarks>
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

    /// <summary>
    /// Marks the transaction as completed, indicating it should be committed when the scope is disposed.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method only affects scopes that own their session. Nested scopes that reuse existing
    /// sessions will not perform any transaction operations when this method is called.
    /// </para>
    /// <para>
    /// If the scope owns a session and supports transactions, calling this method will commit
    /// the transaction immediately. If transactions are not supported, this method has no effect.
    /// </para>
    /// </remarks>
    /// <exception cref="ObjectDisposedException">Thrown if the scope has already been disposed.</exception>
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

    /// <summary>
    /// Disposes the transaction scope and cleans up resources.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method only performs cleanup operations for scopes that own their session.
    /// Nested scopes that reuse existing sessions will not dispose the session or affect transactions.
    /// </para>
    /// <para>
    /// For owned sessions:
    /// </para>
    /// <list type="bullet">
    /// <item><description>If <c>Complete()</c> was not called and transactions are supported, the transaction will be aborted</description></item>
    /// <item><description>The session will be disposed and removed from the current execution context</description></item>
    /// <item><description>The AsyncLocal session reference will be cleared</description></item>
    /// </list>
    /// <para>
    /// For non-owned sessions (nested scopes), this method performs no operations on the session.
    /// </para>
    /// </remarks>
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
