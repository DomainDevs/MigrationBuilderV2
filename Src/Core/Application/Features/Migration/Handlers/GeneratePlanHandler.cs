using Application.Abstractions.Migration;
using Application.Features.Migration.Commands;
using Application.Features.Migration.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.Migration.Handlers;
/*
public sealed class GeneratePlanHandler
    : IRequestHandler<GeneratePlanCommand, MigrationResponseDto>
{
    private readonly IGeneratePlanService _service;

    public GeneratePlanHandler(IGeneratePlanService service)
        => _service = service;

    public Task<MigrationResponseDto> Handle(
        GeneratePlanCommand request,
        CancellationToken cancellationToken)
    {
        return _service.GenerateMigrationPlanAsync(request);
    }

}
*/
public sealed class GeneratePlanHandler
    : IRequestHandler<GeneratePlanCommand, MigrationGenerationResultDto>
{
    private readonly IGeneratePlanService _service;

    public GeneratePlanHandler(IGeneratePlanService service)
        => _service = service;

    public Task<MigrationGenerationResultDto> Handle(
        GeneratePlanCommand request,
        CancellationToken cancellationToken)
    {
        return _service.GenerateMigrationPlanAsync(request);
    }
}