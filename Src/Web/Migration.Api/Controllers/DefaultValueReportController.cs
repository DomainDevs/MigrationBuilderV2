using Microsoft.AspNetCore.Mvc;
using Persistence.Comparison.Services;

namespace Application.Features.Comparison.Controllers;

[ApiController]
[Route("api/comparison/default-values")]
public sealed class DefaultValueReportController : ControllerBase
{
    private readonly DefaultValueReportService _service;

    public DefaultValueReportController(
        DefaultValueReportService service)
    {
        _service = service;
    }

    [HttpGet("{projectName}/{schema}")]
    public async Task<IActionResult> Get(
        string projectName,
        string schema)
    {
        DefaultValueReportService.DefaultValueReport report =
            await _service.GenerateAsync(
                projectName,
                schema);

        return Ok(report);
    }
}

