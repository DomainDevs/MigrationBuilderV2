using Dapper;
using DataToolkit.Library.Engine.Abstractions;
using DataToolkit.Library.Engine.Mapping;
using DataToolkit.Library.Exceptions;
using DataToolkit.Library.Extensions.Resilience;
using Microsoft.Extensions.Logging; //using Serilog;
using Microsoft.Extensions.Logging.Abstractions;
using System.Data;

namespace DataToolkit.Library.Engine.Core;

/// <summary>
/// Ejecuta consultas SQL y procedimientos almacenados usando Dapper,
/// con soporte para interpolación, multi-mapping, multi-result y OUTPUT.
/// </summary>
internal class SqlExecutor : ISqlExecutor, IDisposable
{
    private readonly Func<IDbConnection> _connectionFactory;
    private readonly Func<IDbTransaction?> _transactionProvider;
    private readonly int? _defaultTimeout;
    private readonly ILogger<SqlExecutor> _logger; //private readonly ILogger _logger;
    //private readonly RetryExecutor? _retryExecutor;

    private bool _disposed;

    // ---------------- CONSTRUCTOR MODERNO (UNIT OF WORK LAZY) ----------------
    internal SqlExecutor(
        Func<IDbConnection> connectionFactory,
        Func<IDbTransaction?> transactionProvider, //RetryExecutor? retryExecutor = null,
        int? commandTimeout = null,
        ILogger<SqlExecutor>? logger = null //ILogger? logger = null
        )
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _transactionProvider = transactionProvider ?? (() => null); //_retryExecutor = retryExecutor;
        _defaultTimeout = commandTimeout;
        _logger = logger ?? NullLogger<SqlExecutor>.Instance;  //_logger = logger ?? Log.Logger;
    }

    // =========================================================
    // CORE HELPERS
    // =========================================================

    private IDbConnection Connection => _connectionFactory();
    private IDbTransaction? Tx => _transactionProvider();

    private IDbConnection GetOpenConnection()
    {
        var conn = Connection;

        if (conn.State == ConnectionState.Broken)
            throw new InvalidOperationException("Connection is broken.");

        if (conn.State == ConnectionState.Closed)
            conn.Open();

        return conn;
    }

    private void ValidateSql(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
            throw new ArgumentException("SQL cannot be null or empty.");
    }

    // =========================================================
    // RAW SQL
    // =========================================================

    public IEnumerable<T> FromSql<T>(string sql)
        => FromSql<T>(sql, null, null);

    public IEnumerable<T> FromSql<T>(string sql, object? parameters)
        => FromSql<T>(sql, parameters, null);

    public IEnumerable<T> FromSql<T>(string sql, object? parameters = null, int? commandTimeout = null)
    {
        return ExecuteSafe(() =>
        {
            var conn = GetOpenConnection();

            return conn.Query<T>(
                sql,
                parameters,
                Tx,
                commandTimeout: commandTimeout ?? _defaultTimeout);
        }, sql);
    }

    public async Task<IEnumerable<T>> FromSqlAsync<T>(string sql)
        => await FromSqlAsync<T>(sql, null, null);

    public async Task<IEnumerable<T>> FromSqlAsync<T>(string sql, object? parameters)
        => await FromSqlAsync<T>(sql, parameters, null);

    public async Task<IEnumerable<T>> FromSqlAsync<T>(
        string sql,
        object? parameters = null,
        int? commandTimeout = null,
        CancellationToken ct = default) // <-- Agregar token
    {
        return await ExecuteSafeAsync(async () =>
        {
            var conn = GetOpenConnection();

            return await conn.QueryAsync<T>(new CommandDefinition(
                sql,
                parameters,
                Tx,
                commandTimeout ?? _defaultTimeout,
                cancellationToken: ct)); // <-- Pasar a Dapper
        }, sql);
    }
    // =========================================================
    // DICTIONARY
    // =========================================================

    public IEnumerable<IDictionary<string, object>> FromSqlDictionary(
        string sql,
        object? parameters = null,
        int? commandTimeout = null)
    {
        return ExecuteSafe(() =>
        {
            var conn = GetOpenConnection();

            return conn.Query(sql,
                    parameters,
                    Tx,
                    commandTimeout: commandTimeout ?? _defaultTimeout)
                .Select(r => (IDictionary<string, object>)new Dictionary<string, object>((IDictionary<string, object>)r))
                .ToList();
        }, sql);
    }

    public async Task<IEnumerable<IDictionary<string, object>>> FromSqlDictionaryAsync(
        string sql,
        object? parameters = null,
        int? commandTimeout = null,
        CancellationToken ct = default)
    {
        return await ExecuteSafeAsync(async () =>
        {
            var conn = GetOpenConnection();

            var rows = await conn.QueryAsync(
                new CommandDefinition(
                    sql,
                    parameters,
                    Tx,
                    commandTimeout ?? _defaultTimeout,
                    cancellationToken: ct));

            return rows
                .Select(r => (IDictionary<string, object>)new Dictionary<string, object>((IDictionary<string, object>)r))
                .ToList();
        }, sql);
    }



    // =========================================================
    // INTERPOLATED SQL
    // =========================================================

    public IEnumerable<T> FromSqlInterpolated<T>(FormattableString query)
        => FromSqlInterpolated<T>(query, null);

    public IEnumerable<T> FromSqlInterpolated<T>(FormattableString query, int? commandTimeout = null)
    {
        var (sql, parameters) = BuildInterpolatedSql(query);

        return ExecuteSafe(() =>
        {
            var conn = GetOpenConnection();

            return conn.Query<T>(
                sql,
                parameters,
                Tx,
                commandTimeout: commandTimeout ?? _defaultTimeout);
        }, sql);
    }

    public async Task<IEnumerable<T>> FromSqlInterpolatedAsync<T>(
        FormattableString query,
        int? commandTimeout = null,
        CancellationToken ct = default)
    {
        var (sql, parameters) = BuildInterpolatedSql(query);

        return await ExecuteSafeAsync(async () =>
        {
            var conn = GetOpenConnection();

            return await conn.QueryAsync<T>(
                new CommandDefinition(
                    sql,
                    parameters,
                    Tx,
                    commandTimeout ?? _defaultTimeout,
                    cancellationToken: ct));
        }, sql);
    }

    // =========================================================
    // MULTI MAP
    // =========================================================

    public IEnumerable<T> FromSqlMultiMap<T>(MultiMapRequest<T> request)
        => FromSqlMultiMap<T>(request, null);

    public IEnumerable<T> FromSqlMultiMap<T>(MultiMapRequest<T> request, int? commandTimeout = null)
    {
        return ExecuteSafe(() =>
        {
            var conn = GetOpenConnection();

            var result = conn.Query(
                request.Sql,
                request.Types,
                (object[] objects) => request.MapFunction(objects),
                param: request.Parameters,
                splitOn: request.SplitOn,
                transaction: Tx,
                commandType: CommandType.Text,
                commandTimeout: commandTimeout ?? _defaultTimeout
            );

            return result.Cast<T>();
        }, request.Sql);
    }

    public async Task<IEnumerable<T>> FromSqlMultiMapAsync<T>(
        MultiMapRequest<T> request,
        int? commandTimeout = null)
    {
        return await ExecuteSafeAsync(async () =>
        {
            var conn = GetOpenConnection();

            var result = await conn.QueryAsync(
                request.Sql,
                request.Types,
                objects => request.MapFunction(objects),
                param: request.Parameters,
                splitOn: request.SplitOn,
                transaction: Tx,
                commandType: CommandType.Text,
                commandTimeout: commandTimeout ?? _defaultTimeout
            );

            return result.Cast<T>();
        }, request.Sql);
    }

    // =========================================================
    // QUERY MULTIPLE
    // =========================================================

    public async Task<List<IEnumerable<dynamic>>> QueryMultipleAsync(
        string sql,
        object? parameters = null,
        CommandType commandType = CommandType.StoredProcedure,
        int? commandTimeout = null)
    {
        return await ExecuteSafeAsync(async () =>
        {
            var conn = GetOpenConnection();

            var resultSets = new List<IEnumerable<dynamic>>();

            using var reader = await conn.QueryMultipleAsync(
                sql,
                parameters,
                Tx,
                commandType: commandType,
                commandTimeout: commandTimeout ?? _defaultTimeout);

            while (!reader.IsConsumed)
                resultSets.Add(await reader.ReadAsync());

            return resultSets;
        }, sql);
    }

    // =========================================================
    // EXECUTE
    // =========================================================

    public int Execute(string sql)
        => Execute(sql, null, null);

    public int Execute(string sql, object? parameters)
        => Execute(sql, parameters, null);

    public int Execute(string sql, object? parameters = null, int? commandTimeout = null)
    {
        return ExecuteSafe(() =>
        {
            var conn = GetOpenConnection();

            return conn.Execute(
                sql,
                parameters,
                Tx,
                commandTimeout: commandTimeout ?? _defaultTimeout);
        }, sql);
    }

    public async Task<int> ExecuteAsync(string sql)
        => await ExecuteAsync(sql, null, null);

    public async Task<int> ExecuteAsync(string sql, object? parameters)
        => await ExecuteAsync(sql, parameters, null);

    public async Task<int> ExecuteAsync(
        string sql,
        object? parameters = null,
        int? commandTimeout = null,
        CancellationToken ct = default) // <-- Agregar token
    {
        return await ExecuteSafeAsync(async () =>
        {
            var conn = GetOpenConnection();

            return await conn.ExecuteAsync(new CommandDefinition(
                sql,
                parameters,
                Tx,
                commandTimeout ?? _defaultTimeout,
                cancellationToken: ct)); // <-- Pasar a Dapper
        }, sql);
    }

    // =========================================================
    // OUTPUT
    // =========================================================

    public (int RowsAffected, Dictionary<string, object> OutputValues)
        ExecuteWithOutput(string storedProcedure, Action<DynamicParameters> configureParameters)
        => ExecuteWithOutput(storedProcedure, configureParameters, null);

    public (int RowsAffected, Dictionary<string, object> OutputValues)
        ExecuteWithOutput(
            string storedProcedure,
            Action<DynamicParameters> configureParameters,
            int? commandTimeout = null)
    {
        return ExecuteSafe(() =>
        {
            var conn = GetOpenConnection();

            var parameters = new DynamicParameters();
            configureParameters(parameters);

            var rows = conn.Execute(
                storedProcedure,
                parameters,
                Tx,
                commandType: CommandType.StoredProcedure,
                commandTimeout: commandTimeout ?? _defaultTimeout);

            var output = new Dictionary<string, object>();

            foreach (var name in parameters.ParameterNames)
                output[name] = parameters.Get<object>(name)!;

            return (rows, output);
        }, storedProcedure);
    }

    public async Task<(int RowsAffected, DynamicParameters Output)>
        ExecuteWithOutputAsync(
            string storedProcedure,
            Action<DynamicParameters> configureParameters,
            int? commandTimeout = null)
    {
        return await ExecuteSafeAsync(async () =>
        {
            var conn = GetOpenConnection();

            var parameters = new DynamicParameters();
            configureParameters(parameters);

            var rows = await conn.ExecuteAsync(
                storedProcedure,
                parameters,
                Tx,
                commandType: CommandType.StoredProcedure,
                commandTimeout: commandTimeout ?? _defaultTimeout);

            return (rows, parameters);
        }, storedProcedure);
    }

    // =========================================================
    // INTERPOLATION HELPER
    // =========================================================

    private static (string, DynamicParameters) BuildInterpolatedSql(FormattableString query)
    {
        var dParams = new DynamicParameters();
        var paramNames = new object[query.ArgumentCount];

        for (int i = 0; i < query.ArgumentCount; i++)
        {
            paramNames[i] = $"@p{i}";
            dParams.Add((string)paramNames[i], query.GetArgument(i));
        }

        var sql = string.Format(query.Format, paramNames);
        return (sql, dParams);
    }

    // =========================================================
    // SAFE WRAPPERS
    // =========================================================

    private T ExecuteSafe<T>(Func<T> func, string sql)
    {
        ThrowIfDisposed();
        try
        {
            ValidateSql(sql);
            //if (_retryExecutor is not null) return _retryExecutor.Execute(func);

            return func();
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "SQL execution error. Query Length: {Length}",
                sql?.Length ?? 0);

            #if DEBUG
            // En desarrollo dejamos propagar la excepción original
            // para que Visual Studio rompa exactamente en el origen.
                throw;
            #else
                throw new SqlExecutorException(
                    $"SQL execution error (Length: {sql?.Length ?? 0})",
                    ex);
            #endif
        }
    }

    private async Task<T> ExecuteSafeAsync<T>(Func<Task<T>> func, string sql)
    {
        ThrowIfDisposed();
        try
        {
            ValidateSql(sql);
            //if (_retryExecutor is not null) return await _retryExecutor.ExecuteAsync(func);

            return await func();
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "SQL async execution error. Query Length: {Length}",
                sql?.Length ?? 0);

            #if DEBUG
            // En desarrollo dejamos propagar la excepción original
            // para que Visual Studio rompa exactamente en el origen.

                throw;
            #else
                throw new SqlExecutorException(
                    $"SQL async execution error (Length: {sql?.Length ?? 0})",
                    ex);
            #endif
        }
    }

    // =========================================================
    // DISPOSE
    // =========================================================

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(SqlExecutor));
    }

}