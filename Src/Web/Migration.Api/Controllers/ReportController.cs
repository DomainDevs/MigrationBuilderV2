using Application.Features.Comparison.DTOs;
using Application.Features.Comparison.Handlers;
using Microsoft.AspNetCore.Mvc;
using Application.Features.Comparison.Mappers;

namespace Migration.Api.Controllers;

[ApiController]
[Route("api/comparison")]
public sealed class ReportController : ControllerBase
{
    private readonly CompareMetadataHandler _handler;
    private readonly CompareValidationHandler _validationHandler;

    public ReportController(
        CompareMetadataHandler handler, 
        CompareValidationHandler validationHandler)
    {
        _handler = handler;
        _validationHandler = validationHandler;
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

    [HttpPost("validation")]
    public async Task<IActionResult> CompareValidation(
        [FromBody] CompareValidationRequestDto dto)
    {
        var command = dto.ToCompareValidationCommand();

        var result =
            await _validationHandler.HandleAsync(command);

        return Ok(result);
    }
}
