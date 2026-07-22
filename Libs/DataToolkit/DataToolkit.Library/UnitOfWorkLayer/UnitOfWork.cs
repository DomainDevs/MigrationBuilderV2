using DataToolkit.Library.Connections;
using DataToolkit.Library.Engine.Abstractions;
using DataToolkit.Library.Engine.Core;
using DataToolkit.Library.Repositories;
using System.Data;

namespace DataToolkit.Library.UnitOfWorkLayer;

public sealed class UnitOfWork : IUnitOfWork, IDisposable
{
    private readonly IDbConnectionFactory _factory;
    private readonly string _connection;

    private IDbConnection? _dbConnection;
    private IDbTransaction? _transaction;

    private readonly Dictionary<Type, object> _repositories = new();

    private bool _disposed;

    public IDbTransaction? Transaction => _transaction; 
    public bool HasActiveTransaction => _transaction != null;

    public ISqlExecutor Sql { get; }

    public UnitOfWork(
        IDbConnectionFactory factory,
        string connection = "SqlServer"
        )
    {
        ArgumentNullException.ThrowIfNull(factory);

        _factory = factory;

        ArgumentException.ThrowIfNullOrWhiteSpace(connection);

        _connection = connection;

        // Connection is created lazily - Opening is deferred until first use.
        Sql = new SqlExecutor(
            GetConnection,
            GetTransaction
            );
    }

    // =========================================================
    // SINGLE OWNERSHIP: CONNECTION CONTROLLED ONLY HERE
    // =========================================================
    private IDbConnection GetConnection()
    {
        ThrowIfDisposed();

        if (_dbConnection == null)
        {
            _dbConnection =
                _factory.CreateConnection(_connection)
                ?? throw new InvalidOperationException(
                    "The connection factory returned a null connection.");
        }

        EnsureOpen();
        return _dbConnection;
    }

    private IDbTransaction? GetTransaction() => _transaction;

    private void EnsureOpen()
    {
        if (_dbConnection == null) return;

        if (_dbConnection.State == ConnectionState.Broken)
            throw new InvalidOperationException("Connection is broken.");

        if (_dbConnection.State != ConnectionState.Open)
            _dbConnection.Open();
    }

    // =========================================================
    // TRANSACTIONS
    // =========================================================
    public void BeginTransaction()
    {
        ThrowIfDisposed();

        var conn = GetConnection();

        if (_transaction != null)
            throw new InvalidOperationException("Transaction already active.");

        _transaction = conn.BeginTransaction();
        _repositories.Clear();
    }

    public void Commit()
    {
        ThrowIfDisposed();

        if (_transaction == null)
            throw new InvalidOperationException("No active transaction to commit.");

        try
        {
            _transaction.Commit();
        }
        finally
        {
            ClearTransaction();
        }
    }

    public void Rollback()
    {
        ThrowIfDisposed();

        if (_transaction == null)
            throw new InvalidOperationException("No active transaction to rollback.");

        try
        {
            _transaction.Rollback();
        }
        finally
        {
            ClearTransaction();
        }
    }

    private void ClearTransaction()
    {
        _transaction?.Dispose();
        _transaction = null;
        // Cada transacción obtiene instancias nuevas de repositorios.
        _repositories.Clear();
    }

    // =========================================================
    // REPOSITORIES
    // =========================================================
    public IGenericRepository<T> Repository<T>() where T : class
    {
        ThrowIfDisposed();

        if (_repositories.TryGetValue(typeof(T), out var repo))
            return (IGenericRepository<T>)repo;

        var instance = new GenericRepository<T>(Sql);
        _repositories.Add(typeof(T), instance);

        return instance;
    }

    // =========================================================
    // SAFETY
    // =========================================================
    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(UnitOfWork));
    }

    // =========================================================
    // DISPOSE (OWNERSHIP RULE)
    // =========================================================
    public void Dispose()
    {
        if (_disposed) return;

        try
        {
            _transaction?.Dispose();
            _transaction = null;

            if (Sql is IDisposable disposable)
                disposable.Dispose();

            _dbConnection?.Dispose();
            _dbConnection = null;
            // Release cached repositories.
            _repositories.Clear();
        }
        finally
        {
            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}