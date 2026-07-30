using System.Text;
using DataToolkit.Library.Fluent.Sql;

namespace DataToolkit.Library.Fluent.Compilation;

internal sealed class SqlCompiler
{
    private readonly SqlTrace? _trace;

    public SqlCompiler(SqlTrace? trace = null)
    {
        _trace = trace;
    }

    public string Compile(IEnumerable<SqlNode> nodes)
    {
        ArgumentNullException.ThrowIfNull(nodes);

        Log("[BEGIN] SQL COMPILATION");

        // 1. Clasificación en una sola pasada O(N)
        SqlSelect? select = null;
        SqlFrom? from = null;
        SqlWhere? where = null;
        SqlGroupBy? groupBy = null;
        SqlOrderBy? orderBy = null;
        SqlSkip? skip = null;
        SqlTake? take = null;
        List<SqlJoin>? joins = null;

        foreach (var node in nodes)
        {
            switch (node)
            {
                case SqlSelect s: select ??= s; break;
                case SqlFrom f: from ??= f; break;
                case SqlWhere w: where ??= w; break;
                case SqlGroupBy g: groupBy ??= g; break;
                case SqlOrderBy o: orderBy ??= o; break;
                case SqlSkip sk: skip ??= sk; break;
                case SqlTake tk: take ??= tk; break;
                case SqlJoin j:
                    (joins ??= new List<SqlJoin>()).Add(j);
                    break;
            }
        }

        var sb = new StringBuilder(256);

        // ---------------- SELECT ----------------
        sb.Append("SELECT ");
        if (select is null || select.Columns.Count == 0)
        {
            sb.Append('*');
            Log("[SELECT] *");
        }
        else
        {
            sb.AppendJoin(", ", select.Columns);
            Log(() => $"[SELECT] {string.Join(", ", select.Columns)}");
        }
        sb.AppendLine();

        // ---------------- FROM ----------------
        if (from is null || from.Tables.Count == 0)
            throw new InvalidOperationException("FROM clause is required.");

        sb.Append("FROM ");
        sb.AppendJoin(", ", from.Tables);
        Log(() => $"[FROM] {string.Join(", ", from.Tables)}");

        // ---------------- JOIN ----------------
        var joinCount = joins?.Count ?? 0;
        Log(() => $"[JOIN] COUNT={joinCount}");

        if (joins is not null)
        {
            foreach (var join in joins)
            {
                Log(() => $"[JOIN] {join.Type} {join.Table} ON {join.On}");

                sb.AppendLine()
                  .Append(join.Type)
                  .Append(' ')
                  .Append(join.Table)
                  .Append(" ON ")
                  .Append(join.On);
            }
        }

        // ---------------- WHERE ----------------
        if (where is not null)
        {
            Log("[WHERE] EXISTS");

            sb.AppendLine()
              .Append("WHERE ");

            Render(sb, where.Expression);
        }

        // ---------------- GROUP BY ----------------
        if (groupBy is not null && groupBy.Columns.Count > 0)
        {
            Log(() => $"[GROUP BY] {string.Join(", ", groupBy.Columns)}");

            sb.AppendLine()
              .Append("GROUP BY ");
            sb.AppendJoin(", ", groupBy.Columns);
        }

        // ---------------- ORDER BY ----------------
        if (orderBy is not null && orderBy.Columns.Count > 0)
        {
            Log(() => $"[ORDER BY] {string.Join(", ", orderBy.Columns)}");

            sb.AppendLine()
              .Append("ORDER BY ");
            sb.AppendJoin(", ", orderBy.Columns);
        }

        // ---------------- PAGING ----------------
        if ((skip is not null || take is not null) && orderBy is null)
        {
            throw new InvalidOperationException("Skip() and Take() require OrderBy().");
        }

        if (skip is not null)
        {
            Log(() => $"[SKIP] {skip.Value}");

            sb.AppendLine()
              .Append("OFFSET ")
              .Append(skip.Value)
              .Append(" ROWS");
        }

        if (take is not null)
        {
            Log(() => $"[TAKE] {take.Value}");

            sb.AppendLine()
              .Append("FETCH NEXT ")
              .Append(take.Value)
              .Append(" ROWS ONLY");
        }

        var sql = sb.ToString();

        Log("[END] SQL COMPILATION");
        Log(() => $"[SQL] {sql}");

        return sql;
    }

    private void Render(StringBuilder sb, SqlNode node)
    {
        switch (node)
        {
            case SqlRaw r:
                Log(() => $"[RAW] {r.Text}");
                sb.Append(r.Text);
                break;

            case SqlParameter p:
                Log(() => $"[PARAMETER] {p.Name} = {p.Value ?? "NULL"}");
                sb.Append(p.Name);
                break;

            case SqlBinary b:
                Log(() => $"[BINARY] {b.Op}");

                sb.Append('(');
                Render(sb, b.Left);
                sb.Append(' ')
                  .Append(b.Op)
                  .Append(' ');
                Render(sb, b.Right);
                sb.Append(')');
                break;

            case SqlWhere w:
                Render(sb, w.Expression);
                break;
        }
    }

    // Métodos de logging limpios
    private void Log(string message)
    {
        if (_trace?.Enabled == true)
            _trace.Add(message);
    }

    private void Log(Func<string> messageFactory)
    {
        if (_trace?.Enabled == true)
            _trace.Add(messageFactory());
    }
}