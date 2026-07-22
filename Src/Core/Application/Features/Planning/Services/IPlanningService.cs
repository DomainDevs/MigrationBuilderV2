
namespace Application.Features.Planning.Services
{
    public interface IPlanningService
    {
        Task<bool> PingAsync();
    }
}