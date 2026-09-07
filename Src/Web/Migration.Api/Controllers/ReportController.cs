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
    private readonly CompareRecordCountHandler _recordCountHandler;
    private readonly CompareRecordValueHandler _recordValueHandler;

    public ReportController(
        CompareMetadataHandler handler,
        CompareValidationHandler validationHandler,
        CompareRecordCountHandler recordCountHandler,
        CompareRecordValueHandler recordValueHandler)
    {
        _handler = handler;
        _validationHandler = validationHandler;
        _recordCountHandler = recordCountHandler;
        _recordValueHandler = recordValueHandler;
        _recordValueHandler = recordValueHandler;
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

    [HttpPost("record-count")]
    public async Task<IActionResult> CompareRecordCount(
        [FromBody] RecordCountRequestDto dto)
    {
        var command = dto.ToCompareRecordCountCommand();

        var result =
            await _recordCountHandler.HandleAsync(command);

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

    [HttpPost("record-value")]
    public async Task<IActionResult> CompareRecordValue(
        [FromBody] RecordValueRequestDto dto)
    {
        var command = dto.ToCompareRecordValueCommand();

        var result =
            await _recordValueHandler.HandleAsync(command);

        return Ok(result);
    }

}
