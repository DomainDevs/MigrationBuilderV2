using Application.Features.Execution.Commands;
using Application.Features.Execution.DTOs;
using Application.Features.Execution.Mappers;
using Application.Features.Execution.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Shared.DTOs;
using Shared.Helpers;
using System.Linq.Expressions;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
[ApiExplorerSettings(IgnoreApi = true)] //no se vea en swagger
public class PackageExecutionController : ControllerBase
{
    private readonly IMediator _mediator;

    public PackageExecutionController(IMediator mediator)
    {
        _mediator = mediator;
    }

    
    // =====================================
    // GET: api/PackageExecution
    // =====================================
    [HttpGet]
    [ProducesResponseType(typeof(ResponseDTO<IEnumerable<PackageExecutionQueryResponseDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var list = await _mediator.Send(new PackageExecutionGetAllQuery());
        return Ok(ApiResponse.Success(list, "Consulta exitosa"));
    }

    // =====================================
    // GET: api/PackageExecution/{executionid}
    // =====================================
    [HttpGet("{executionid}")]
    [ProducesResponseType(typeof(ResponseDTO<IEnumerable<PackageExecutionQueryResponseDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(int executionid)
    {
        var item = await _mediator.Send(new PackageExecutionGetByIdQuery(executionid));
        if (item == null) return NotFound(ApiResponse.Fail<object>("Registro no encontrado"));
        return Ok(ApiResponse.Success(item, "Registro encontrado"));
    }

    // =====================================
    // POST: api/PackageExecution
    // =====================================
    [HttpPost]
    [ProducesResponseType(typeof(ResponseDTO<object>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ResponseDTO<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] PackageExecutionCreateRequestDto dto)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return BadRequest(ApiResponse.Fail("Error de validación", errors));
        }
        var command = dto.ToCommandCreate();
        var result = await _mediator.Send(command);

        if (result == 0)
        {
            return BadRequest(ApiResponse.Fail<object>("No se pudo insertar el registro"));
        }
        return CreatedAtAction(nameof(GetById), new { executionid = result }, ApiResponse.Success(result, "Registro creado correctamente"));
    }

    // =====================================
    // PUT: api/PackageExecution/{executionid}
    // =====================================
    [HttpPut("{executionid}")]
    [ProducesResponseType(typeof(ResponseDTO<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseDTO<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ResponseDTO<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int executionid, [FromBody] PackageExecutionUpdateRequestDto dto)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return BadRequest(ApiResponse.Fail("Error de validación", errors));
        }

        //Inyección de llaves
        dto.ExecutionId = executionid;

        var command = dto.ToUpdateCommand();
        var result = await _mediator.Send(command);
        if (result == 0)
        {
            return NotFound(ApiResponse.Fail<object>("Registro no encontrado para actualización"));
        }
        return Ok(ApiResponse.Success(result, "Registro actualizado correctamente"));
    }

    // =====================================
    // DELETE: api/PackageExecution/{executionid}
    // =====================================
    [HttpDelete("{executionid}")]
    [ProducesResponseType(typeof(ResponseDTO<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseDTO<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int executionid)
    {
        var deleted = await _mediator.Send(new PackageExecutionDeleteCommand(executionid));
        if (!deleted)
        {
            return NotFound(ApiResponse.Fail<object>("Registro no encontrado para eliminación"));
        }
        return Ok(ApiResponse.Success<object>(null, "Registro eliminado correctamente"));
    }
    
    
}