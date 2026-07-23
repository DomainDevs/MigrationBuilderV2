namespace DataToolkit.Bootstrap.Exceptions;

public sealed class BootstrapConfigurationException : Exception
{
    public BootstrapConfigurationException(string message)
        : base(message)
    {
    }

    public BootstrapConfigurationException(
        string message,
        Exception innerException)
        : base(message, innerException)
    {
    }
}