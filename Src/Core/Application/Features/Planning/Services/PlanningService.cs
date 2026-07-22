namespace Application.Features.Planning.Services;
public sealed class PlanningService : IPlanningService
{
    public Task<bool> PingAsync()
    {
        return Task.FromResult(true);
    }
}