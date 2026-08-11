using Application.Features.Comparison.DTOs;
using Application.Features.Comparison.Handlers;
using Microsoft.AspNetCore.Mvc;
using Application.Features.Comparison.Mappers;

namespace Migration.Api.Controllers;

[ApiController]
[Route("api/comparison")]
public sealed class ComparisonController : ControllerBase
{
    private readonly CompareMetadataHandler _handler;

    public ComparisonController(
        CompareMetadataHandler handler)
    {
        _handler = handler;
    }

    [HttpPost("metadata")]
    public async Task<IActionResult> CompareMetadata(
        [FromBody] MetadataRequestDto dto)
    {
        var command = dto.ToCompareMetadataCommand();

        var result =
            await _handler.HandleAsync(command);

        return Ok(result);
    }
}
