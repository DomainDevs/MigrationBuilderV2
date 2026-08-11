using Application.Abstractions.Migration;
using Application.Features.Migration.Commands;
using Application.Features.Migration.DTOs;
using MediatR;

namespace Application.Features.Migration.Handlers;

public sealed class GenerateValidationHandler
    : IRequestHandler<GenerateValidationCommand, MigrationResponseDto>
{
    private readonly IGenerateValidationService _service;

    public GenerateValidationHandler(
        IGenerateValidationService service)
        => _service = service;

    public Task<MigrationResponseDto> Handle(
        GenerateValidationCommand request,
        CancellationToken cancellationToken)
    {
        return _service.GenerateValidationAsync(request);
    }
}