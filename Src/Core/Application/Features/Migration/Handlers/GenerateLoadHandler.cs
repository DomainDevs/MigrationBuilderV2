using Application.Abstractions.Migration;
using Application.Features.Migration.Commands;
using Application.Features.Migration.DTOs;
using MediatR;

namespace Application.Features.Migration.Handlers;

public sealed class GenerateLoadHandler
    : IRequestHandler<GenerateLoadCommand, MigrationResponseDto>
{
    private readonly IGenerateLoadService _service;

    public GenerateLoadHandler(IGenerateLoadService service)
        => _service = service;

    public Task<MigrationResponseDto> Handle(
        GenerateLoadCommand request,
        CancellationToken cancellationToken)
    {
        return _service.GenerateLoadAsync(request);
    }

}