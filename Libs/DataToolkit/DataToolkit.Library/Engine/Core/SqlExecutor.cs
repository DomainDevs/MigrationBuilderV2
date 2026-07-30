using System.Data;
using System.Data.Common;
using Dapper;
using DataToolkit.Library.Engine.Abstractions;
using DataToolkit.Library.Engine.Mapping;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

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
    private readonly ILogger<SqlExecutor> _logger;

    private bool _disposed;

    // ---------------- CONSTRUCTOR (UNIT OF WORK LAZY) ----------------
    internal SqlExecutor(
        Func<IDbConnection> connectionFactory,
        Func<IDbTransaction?> transactionProvider,
        int? commandTimeout = null,
        ILogger<SqlExecutor>? logger = null)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _transactionProvider = transactionProvider ?? (() => null);
        _defaultTimeout = commandTimeout;
        _logger = logger ?? NullLogger<SqlExecutor>.Instance;
    }

    // =========================================================
    // CORE HELPERS (CENTRALIZADOS Y UNIFICADOS)
    // =========================================================

    private IDbConnection Connection => _connectionFactory();
    private IDbTransaction? Tx => _transactionProvider();

    /// <summary>
    /// Valida el estado de la conexión centralizando la regla de negocio.
    /// Evita duplicar lógica entre flujos síncronos y asíncronos.
    /// </summary>
    private IDbConnection GetConnectionAndValidate()
    {
        var conn = Connection;

        if (conn.State == ConnectionState.Broken)
            throw new InvalidOperationException("Connection is broken.");

        return conn;
    }

    private IDbConnection GetOpenConnection()
    {
        var conn = GetConnectionAndValidate();

        if (conn.State == ConnectionState.Closed)
            conn.Open();

        return conn;
    }

    /// <summary>
    /// Abre la conexión de forma asíncrona real si el proveedor hereda de DbConnection,
    /// evitando bloquear el ThreadPool durante la fase de negociación de red.
    /// </summary>
    private async Task<IDbConnection> GetOpenConnectionAsync(CancellationToken ct = default)
    {
        var conn = GetConnectionAndValidate();

        if (conn.State == ConnectionState.Closed)
        {
            if (conn is DbConnection dbConn)
                await dbConn.OpenAsync(ct).ConfigureAwait(false);
            else
                conn.Open();
        }

        return conn;
    }

    private static void ValidateSql(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
            throw new ArgumentException("SQL cannot be null or empty.", nameof(sql));
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

    public Task<IEnumerable<T>> FromSqlAsync<T>(string sql)
        => FromSqlAsync<T>(sql, null, null);

    public Task<IEnumerable<T>> FromSqlAsync<T>(string sql, object? parameters)
        => FromSqlAsync<T>(sql, parameters, null);

    public async Task<IEnumerable<T>> FromSqlAsync<T>(
        string sql,
        object? parameters = null,
        int? commandTimeout = null,
        CancellationToken ct = default)
    {
        return await ExecuteSafeAsync(async () =>
        {
            var conn = await GetOpenConnectionAsync(ct).ConfigureAwait(false);

            return await conn.QueryAsync<T>(new CommandDefinition(
                sql,
                parameters,
                Tx,
                commandTimeout ?? _defaultTimeout,
                cancellationToken: ct)).ConfigureAwait(false);
        }, sql, ct).ConfigureAwait(false);
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
                .Select(r => (IDictionary<string, object>)r)
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
            var conn = await GetOpenConnectionAsync(ct).ConfigureAwait(false);

            var rows = await conn.QueryAsync(
                new CommandDefinition(
                    sql,
                    parameters,
                    Tx,
                    commandTimeout ?? _defaultTimeout,
                    cancellationToken: ct)).ConfigureAwait(false);

            return rows
                .Select(r => (IDictionary<string, object>)r)
                .ToList();
        }, sql, ct).ConfigureAwait(false);
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
            var conn = await GetOpenConnectionAsync(ct).ConfigureAwait(false);

            return await conn.QueryAsync<T>(
                new CommandDefinition(
                    sql,
                    parameters,
                    Tx,
                    commandTimeout ?? _defaultTimeout,
                    cancellationToken: ct)).ConfigureAwait(false);
        }, sql, ct).ConfigureAwait(false);
    }

    // =========================================================
    // MULTI MAP
    // =========================================================

    public IEnumerable<T> FromSqlMultiMap<T>(MultiMapRequest<T> request)
        => FromSqlMultiMap<T>(request, null);

    public IEnumerable<T> FromSqlMultiMap<T>(MultiMapRequest<T> request, int? commandTimeout = null)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));

        return ExecuteSafe(() =>
        {
            var conn = GetOpenConnection();

            var result = conn.Query(
                request.Sql,
                request.Types,
                request.MapFunction,
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
        if (request is null) throw new ArgumentNullException(nameof(request));

        return await ExecuteSafeAsync(async () =>
        {
            var conn = await GetOpenConnectionAsync().ConfigureAwait(false);

            var result = await conn.QueryAsync(
                request.Sql,
                request.Types,
                request.MapFunction,
                param: request.Parameters,
                splitOn: request.SplitOn,
                transaction: Tx,
                commandType: CommandType.Text,
                commandTimeout: commandTimeout ?? _defaultTimeout
            ).ConfigureAwait(false);

            return result.Cast<T>();
        }, request.Sql).ConfigureAwait(false);
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
            var conn = await GetOpenConnectionAsync().ConfigureAwait(false);

            var resultSets = new List<IEnumerable<dynamic>>();

            using var reader = await conn.QueryMultipleAsync(
                sql,
                parameters,
                Tx,
                commandType: commandType,
                commandTimeout: commandTimeout ?? _defaultTimeout).ConfigureAwait(false);

            while (!reader.IsConsumed)
            {
                resultSets.Add(await reader.ReadAsync().ConfigureAwait(false));
            }

            return resultSets;
        }, sql).ConfigureAwait(false);
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

    public Task<int> ExecuteAsync(string sql)
        => ExecuteAsync(sql, null, null);

    public Task<int> ExecuteAsync(string sql, object? parameters)
        => ExecuteAsync(sql, parameters, null);

    public async Task<int> ExecuteAsync(
        string sql,
        object? parameters = null,
        int? commandTimeout = null,
        CancellationToken ct = default)
    {
        return await ExecuteSafeAsync(async () =>
        {
            var conn = await GetOpenConnectionAsync(ct).ConfigureAwait(false);

            return await conn.ExecuteAsync(new CommandDefinition(
                sql,
                parameters,
                Tx,
                commandTimeout ?? _defaultTimeout,
                cancellationToken: ct)).ConfigureAwait(false);
        }, sql, ct).ConfigureAwait(false);
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
        if (configureParameters is null) throw new ArgumentNullException(nameof(configureParameters));

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

            var paramNames = parameters.ParameterNames.ToList();
            var output = new Dictionary<string, object>(paramNames.Count, StringComparer.OrdinalIgnoreCase);

            foreach (var name in paramNames)
            {
                output[name] = parameters.Get<object>(name)!;
            }

            return (rows, output);
        }, storedProcedure);
    }

    public async Task<(int RowsAffected, Dictionary<string, object> OutputValues)> ExecuteWithOutputAsync(
        string storedProcedure,
        Action<DynamicParameters> configureParameters,
        int? commandTimeout = null)
    {
        if (configureParameters is null) throw new ArgumentNullException(nameof(configureParameters));

        return await ExecuteSafeAsync(async () =>
        {
            var conn = await GetOpenConnectionAsync().ConfigureAwait(false);

            var parameters = new DynamicParameters();
            configureParameters(parameters);

            var rows = await conn.ExecuteAsync(
                storedProcedure,
                parameters,
                Tx,
                commandType: CommandType.StoredProcedure,
                commandTimeout: commandTimeout ?? _defaultTimeout).ConfigureAwait(false);

            var paramNames = parameters.ParameterNames.ToList();
            var output = new Dictionary<string, object>(paramNames.Count, StringComparer.OrdinalIgnoreCase);

            foreach (var name in paramNames)
            {
                output[name] = parameters.Get<object>(name)!;
            }

            return (rows, output);
        }, storedProcedure).ConfigureAwait(false);
    }

    // =========================================================
    // INTERPOLATION HELPER
    // =========================================================

    private static (string Sql, DynamicParameters Parameters) BuildInterpolatedSql(FormattableString query)
    {
        if (query is null) throw new ArgumentNullException(nameof(query));

        var argCount = query.ArgumentCount;
        var dParams = new DynamicParameters();
        var paramNames = new object[argCount];

        for (int i = 0; i < argCount; i++)
        {
            var pName = $"@p{i}";
            paramNames[i] = pName;
            dParams.Add(pName, query.GetArgument(i));
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
            return func();
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "SQL execution error. Query Length: {Length}",
                sql?.Length ?? 0);

            throw;
        }
    }

    private async Task<T> ExecuteSafeAsync<T>(Func<Task<T>> func, string sql, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        ct.ThrowIfCancellationRequested();

        try
        {
            ValidateSql(sql);
            return await func().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "SQL async execution error. Query Length: {Length}",
                sql?.Length ?? 0);

            throw;
        }
    }

    // =========================================================
    // DISPOSE
    // =========================================================

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(SqlExecutor));
    }
}