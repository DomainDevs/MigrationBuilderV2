using Application.Features.Orchestrator.DTOs.Requests;
using Application.Features.Orchestrator.DTOs.Responses;
using Application.Features.Orchestrator.Mappers;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Migration.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class OrchestratorController : ControllerBase
{
    private readonly IMediator _mediator;

    public OrchestratorController(
        IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("execute")]
    [ProducesResponseType(typeof(MigrationExecuteResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<MigrationExecuteResponse>> ExecuteAsync(
        [FromBody] MigrationExecuteRequest request)
    {
        MigrationExecuteResponse response =
            await _mediator.Send(
                request.ToCommand());

        return Ok(response);
    }
}