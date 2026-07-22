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
        var nodeList = nodes.ToList();
        var sb = new StringBuilder();

        Log("[BEGIN] SQL COMPILATION");

        // ---------------- SELECT ----------------
        var select = nodeList.OfType<SqlSelect>().FirstOrDefault();

        var selectSql = select is null || select.Columns.Count == 0
            ? "*"
            : string.Join(", ", select.Columns);

        Log($"[SELECT] {selectSql}");

        sb.Append("SELECT ")
          .Append(selectSql)
          .AppendLine();

        // ---------------- FROM ----------------
        var from = nodeList.OfType<SqlFrom>().FirstOrDefault();

        if (from is null)
            throw new InvalidOperationException(
                "FROM clause is required");

        var fromSql = string.Join(", ", from.Tables);

        Log($"[FROM] {fromSql}");

        sb.Append("FROM ")
          .Append(fromSql)
          .AppendLine();

        // ---------------- JOIN ----------------
        var joins = nodeList.OfType<SqlJoin>().ToList();

        Log($"[JOIN] COUNT={joins.Count}");

        foreach (var join in joins)
        {
            Log($"[JOIN] {join.Type} {join.Table} ON {join.On}");

            sb.Append(join.Type)
              .Append(" ")
              .Append(join.Table)
              .Append(" ON ")
              .Append(join.On)
              .AppendLine();
        }

        // ---------------- WHERE ----------------
        var where = nodeList.OfType<SqlWhere>().FirstOrDefault();

        if (where is not null)
        {
            Log("[WHERE] EXISTS");

            sb.Append("WHERE ");

            Render(sb, where.Expression);

            sb.AppendLine();
        }

        // ---------------- GROUP BY ----------------
        var groupBy = nodeList.OfType<SqlGroupBy>().FirstOrDefault();

        if (groupBy is not null &&
            groupBy.Columns.Count > 0)
        {
            var gb = string.Join(", ", groupBy.Columns);

            Log($"[GROUP BY] {gb}");

            sb.Append("GROUP BY ")
              .Append(gb)
              .AppendLine();
        }

        // ---------------- ORDER BY ----------------
        var orderBy = nodeList.OfType<SqlOrderBy>().FirstOrDefault();

        if (orderBy is not null &&
            orderBy.Columns.Count > 0)
        {
            var ob = string.Join(", ", orderBy.Columns);

            Log($"[ORDER BY] {ob}");

            sb.Append("ORDER BY ")
              .Append(ob);
        }

        // ---------------- PAGING ----------------
        var skip = nodeList
            .OfType<SqlSkip>()
            .FirstOrDefault();

        var take = nodeList
            .OfType<SqlTake>()
            .FirstOrDefault();

        if ((skip is not null || take is not null)
            && orderBy is null)
        {
            throw new InvalidOperationException(
                "Skip() and Take() require OrderBy().");
        }

        if (skip is not null)
        {
            Log($"[SKIP] {skip.Value}");

            sb.AppendLine()
              .Append($"OFFSET {skip.Value} ROWS");
        }

        if (take is not null)
        {
            Log($"[TAKE] {take.Value}");

            sb.AppendLine()
              .Append($"FETCH NEXT {take.Value} ROWS ONLY");
        }

        var sql = sb.ToString().Trim();

        Log("[END] SQL COMPILATION");
        Log($"[SQL] {sql}");

        return sql;
    }

    private void Render(
        StringBuilder sb,
        SqlNode node)
    {
        switch (node)
        {
            case SqlRaw r:

                Log($"[RAW] {r.Text}");

                sb.Append(r.Text);
                break;

            case SqlBinary b:

                Log($"[BINARY] {b.Op}");

                sb.Append("(");

                Render(sb, b.Left);

                sb.Append(" ")
                  .Append(b.Op)
                  .Append(" ");

                Render(sb, b.Right);

                sb.Append(")");

                break;

            case SqlWhere w:

                Render(sb, w.Expression);

                break;
        }
    }

    private void Log(string message)
    {
        _trace?.Add(message);
    }
}