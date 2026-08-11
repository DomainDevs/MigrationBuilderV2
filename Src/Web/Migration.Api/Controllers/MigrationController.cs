using Application.Features.Migration.Commands;
using Application.Features.Migration.DTOs;
using Application.Features.Migration.Mappers;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Shared.DTOs;
using Shared.Helpers;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MigrationController : ControllerBase
{
    private readonly IMediator _mediator;

    public MigrationController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // =====================================
    // POST: api/Migration/Plan
    // =====================================
    [HttpPost("Plan")]
    [ProducesResponseType(typeof(ResponseDTO<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseDTO<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GeneratePlan([FromBody] MigrationRequestDto dto)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();

            return BadRequest(ApiResponse.Fail("Error de validación", errors));
        }

        var result = await _mediator.Send(dto.ToGeneratePlanCommand());

        return Ok(ApiResponse.Success(result, "Script de carga generado correctamente"));
    }

    // =====================================
    // POST: api/Migration/ddl
    // =====================================
    [HttpPost("ddl")]
    [ProducesResponseType(typeof(ResponseDTO<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseDTO<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GenerateDdl([FromBody] MigrationRequestDto dto)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();

            return BadRequest(ApiResponse.Fail("Error de validación", errors));
        }

        var result = await _mediator.Send(dto.ToGenerateDdlCommand());

        return Ok(ApiResponse.Success(result, "DDL generado correctamente"));
    }

    
    // =====================================
    // POST: api/Migration/extraction
    // =====================================
    [HttpPost("extraction")]
    [ProducesResponseType(typeof(ResponseDTO<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseDTO<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GenerateExtraction([FromBody] MigrationRequestDto dto)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();

            return BadRequest(ApiResponse.Fail("Error de validación", errors));
        }

        var result = await _mediator.Send(dto.ToGenerateExtractionCommand());

        return Ok(ApiResponse.Success(result, "Script de extracción generado correctamente"));
    }



    // =====================================
    // POST: api/Migration/Load
    // =====================================
    [HttpPost("load")]
    [ProducesResponseType(typeof(ResponseDTO<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseDTO<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GenerateLoad([FromBody] MigrationRequestDto dto)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();

            return BadRequest(ApiResponse.Fail("Error de validación", errors));
        }

        var result = await _mediator.Send(dto.ToGenerateLoadCommand());

        return Ok(ApiResponse.Success(result, "Script de carga generado correctamente"));
    }

    // =====================================
    // POST: api/Migration/validation
    // =====================================
    [HttpPost("validation")]
    [ProducesResponseType(typeof(ResponseDTO<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseDTO<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GenerateValidation(
        [FromBody] MigrationRequestDto dto)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();

            return BadRequest(
                ApiResponse.Fail(
                    "Error de validación",
                    errors));
        }

        var result =
            await _mediator.Send(
                dto.ToGenerateValidationCommand());

        return Ok(
            ApiResponse.Success(
                result,
                "Script de validación generado correctamente"));
    }

    /*
    // =====================================
    // POST: api/Migration/execution
    // =====================================
    [HttpPost("execution")]
    [ProducesResponseType(typeof(ResponseDTO<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseDTO<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GenerateExecution([FromBody] MigrationRequestDto dto)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();

            return BadRequest(ApiResponse.Fail("Error de validación", errors));
        }

        var result = await _mediator.Send(dto.ToGenerateExecutionCommand());

        return Ok(ApiResponse.Success(result, "Script de ejecución generado correctamente"));
    }

    // =====================================
    // POST: api/Migration/homologation
    // =====================================
    [HttpPost("homologation")]
    [ProducesResponseType(typeof(ResponseDTO<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseDTO<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GenerateHomologation([FromBody] MigrationRequestDto dto)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();

            return BadRequest(ApiResponse.Fail("Error de validación", errors));
        }

        var result = await _mediator.Send(dto.ToGenerateHomologationCommand());

        return Ok(ApiResponse.Success(result, "Script de homologación generado correctamente"));
    }

    */

}