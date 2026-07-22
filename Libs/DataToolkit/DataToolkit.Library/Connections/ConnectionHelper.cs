namespace DataToolkit.Library.Connections;

/// <summary>
/// Provides helper methods for working with ADO.NET connection strings.
/// </summary>
public static class ConnectionHelper
{
    /// <summary>
    /// Determines whether the supplied value appears to be
    /// an ADO.NET connection string.
    /// </summary>
    public static bool IsConnectionString(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        return value.IndexOf('=') > 0;
    }
}