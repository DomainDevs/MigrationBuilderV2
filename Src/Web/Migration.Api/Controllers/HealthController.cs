using Microsoft.AspNetCore.Mvc;
using Persistence.Health.DTOs;
using Persistence.Health.Services;

namespace Migration.Api.Controllers;

[ApiController]
[Route("api/health")]
public sealed class HealthController : ControllerBase
{
    private readonly DatabaseHealthService _healthService;

    public HealthController(
        DatabaseHealthService healthService)
    {
        _healthService = healthService;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        IReadOnlyList<DatabaseHealthResult> results =
            await _healthService.CheckAsync();

        return Ok(results);
    }
}