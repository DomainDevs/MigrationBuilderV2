namespace DataToolkit.Library.Diagnostics;

internal static class SqlPreview
{
    /// <summary>
    /// Renders a SQL statement by replacing parameter placeholders with their
    /// corresponding values for debugging and logging purposes.
    ///
    /// The returned SQL is intended for display only and must never be executed.
    /// </summary>
    public static string Render(string sql, object? parameters)
    {
        if (parameters is null)
            return sql;

        var result = sql;

        foreach (var prop in parameters.GetType().GetProperties())
        {
            var value = prop.GetValue(parameters);

            string formattedValue = value switch
            {
                string s => $"'{s.Replace("'", "''")}'",
                DateTime dt => $"'{dt:yyyy-MM-dd HH:mm:ss}'",
                bool b => b ? "1" : "0",
                null => "NULL",
                _ => value?.ToString() ?? "NULL"
            };

            result = result.Replace("@" + prop.Name, formattedValue);
        }

        return result;
    }
}