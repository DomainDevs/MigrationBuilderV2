using DataToolkit.Library;

namespace Persistence.Migration.Builders;

internal static class LoadBuilder
{
    #region BuildInsert

    public static string BuildInsert(
        TableMetadata metadata,
        bool addParams = false)
    {
        var columns = GetLoadColumns(metadata).ToList();

        var columnNames =
            columns.Select(c => $"[{c.Name}]");

        var parameters =
            columns.Select(c => $"@{c.Name}");

        if (addParams)
        {
            return $"""
INSERT INTO [{metadata.Schema}].[{metadata.Name}]
(
    {string.Join(", ", columnNames)}
)
VALUES
(
    {string.Join(", ", parameters)}
)
""";
        }

        return $"""
INSERT INTO [{metadata.Schema}].[{metadata.Name}]
(
    {string.Join(", ", columnNames)}
)
""";
    }

    #endregion

    #region BuildColumnList

    public static string BuildColumnList(
        TableMetadata metadata)
    {
        return string.Join(
            ", ",
            GetLoadColumns(metadata)
                .Select(c => $"[{c.Name}]"));
    }

    #endregion

    #region GetLoadColumns

    private static IEnumerable<ColumnMetadata> GetLoadColumns(
        TableMetadata metadata)
    {
        return metadata.Columns
            .Where(c => !c.IsComputed);
    }

    #endregion
}