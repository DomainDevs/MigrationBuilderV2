namespace DataToolkit.Library.Extensions.Resilience;

public sealed class RetryExecutor
{
    private readonly IRetryPolicy _policy;

    private static readonly TimeSpan MaxDelay = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan MinDelay = TimeSpan.Zero;

    public RetryExecutor(IRetryPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);

        _policy = policy;
    }

    // =========================================================
    // SYNC - ACTION
    // =========================================================

    public void Execute(Action action)
    {
        if (action is null)
            throw new ArgumentNullException(nameof(action));

        var attempt = 0;

        while (true)
        {
            try
            {
                action();
                return;
            }
            catch (Exception ex)
            {
                attempt++;

                if (!_policy.ShouldRetry(ex, attempt))
                    throw;

                var delay =
                    NormalizeDelay(
                        _policy.GetDelay(attempt));

                Thread.Sleep(delay);
            }
        }
    }

    // =========================================================
    // SYNC - FUNC<T>
    // =========================================================

    public T Execute<T>(Func<T> action)
    {
        if (action is null)
            throw new ArgumentNullException(nameof(action));

        var attempt = 0;

        while (true)
        {
            try
            {
                return action();
            }
            catch (Exception ex)
            {
                attempt++;

                if (!_policy.ShouldRetry(ex, attempt))
                    throw;

                var delay =
                    NormalizeDelay(
                        _policy.GetDelay(attempt));

                Thread.Sleep(delay);
            }
        }
    }

    // =========================================================
    // ASYNC - TASK
    // =========================================================

    public Task ExecuteAsync(Func<Task> action)
    {
        if (action is null)
            throw new ArgumentNullException(nameof(action));

        return ExecuteInternalAsync(action);
    }

    private async Task ExecuteInternalAsync(Func<Task> action)
    {
        var attempt = 0;

        while (true)
        {
            try
            {
                await action().ConfigureAwait(false);
                return;
            }
            catch (Exception ex)
            {
                attempt++;

                if (!_policy.ShouldRetry(ex, attempt))
                    throw;

                var delay =
                    NormalizeDelay(
                        _policy.GetDelay(attempt));

                await Task.Delay(delay)
                    .ConfigureAwait(false);
            }
        }
    }

    // =========================================================
    // ASYNC - TASK<T>
    // =========================================================

    public Task<T> ExecuteAsync<T>(Func<Task<T>> action)
    {
        if (action is null)
            throw new ArgumentNullException(nameof(action));

        return ExecuteInternalAsync(action);
    }

    private async Task<T> ExecuteInternalAsync<T>(
        Func<Task<T>> action)
    {
        var attempt = 0;

        while (true)
        {
            try
            {
                return await action()
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                attempt++;

                if (!_policy.ShouldRetry(ex, attempt))
                    throw;

                var delay =
                    NormalizeDelay(
                        _policy.GetDelay(attempt));

                await Task.Delay(delay)
                    .ConfigureAwait(false);
            }
        }
    }

    // =========================================================
    // HELPERS
    // =========================================================

    private static TimeSpan NormalizeDelay(TimeSpan delay)
    {
        if (delay <= MinDelay)
            return MinDelay;

        if (delay >= MaxDelay)
            return MaxDelay;

        return delay;
    }
}