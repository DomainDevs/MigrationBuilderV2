namespace DataToolkit.Library.Extensions.Resilience;

public interface IRetryPolicy
{
    bool ShouldRetry(Exception ex, int attempt);
    TimeSpan GetDelay(int attempt);
}