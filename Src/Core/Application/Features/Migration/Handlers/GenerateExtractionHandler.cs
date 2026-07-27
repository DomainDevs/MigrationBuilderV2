using Application.Abstractions.Migration;
using Application.Features.Migration.Commands;
using Application.Features.Migration.DTOs;
using MediatR;

namespace Application.Features.Migration.Handlers;
public sealed class GenerateExtractionHandler
    : IRequestHandler<GenerateExtractionCommand, MigrationResponseDto>
{
    private readonly IGenerateExtractionService _service;

    public GenerateExtractionHandler(IGenerateExtractionService service)
        => _service = service;

    public Task<MigrationResponseDto> Handle(
        GenerateExtractionCommand request,
        CancellationToken cancellationToken)
    {
        return _service.GenerateExtractionAsync(request);
    }

}