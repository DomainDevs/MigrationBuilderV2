using Application.Features.Project.Commands;
//using Application.Features.Project.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Migration.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProjectController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProjectController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Crea un nuevo proyecto.
    /// </summary>
    [HttpPost("{projectName}")]
    public async Task<IActionResult> CreateProject(string projectName)
    {
        bool result = await _mediator.Send(
            new ProjectCreateCommand(projectName));

        if (!result)
            return BadRequest();

        return Ok();
    }

    /// <summary>
    /// Elimina un proyecto existente.
    /// </summary>
    [HttpDelete("{projectName}")]
    public async Task<IActionResult> DeleteProject(string projectName)
    {
        bool result = await _mediator.Send(
            new ProjectDeleteCommand(projectName));

        if (!result)
            return NotFound();

        return NoContent();
    }

    /*
    /// <summary>
    /// Lista todos los proyectos.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetProjects()
    {
        var result = await _mediator.Send(
            new ProjectGetAllQuery());

        return Ok(result);
    }

    /// <summary>
    /// Lista los archivos de un proyecto.
    /// </summary>
    [HttpGet("{projectName}/files")]
    public async Task<IActionResult> GetFiles(string projectName)
    {
        var result = await _mediator.Send(
            new ProjectGetFilesQuery(projectName));

        return Ok(result);
    }

    /// <summary>
    /// Obtiene la estructura completa de un proyecto.
    /// </summary>
    [HttpGet("{projectName}/tree")]
    public async Task<IActionResult> GetTree(string projectName)
    {
        var result = await _mediator.Send(
            new ProjectGetTreeQuery(projectName));

        return Ok(result);
    }
    */
}