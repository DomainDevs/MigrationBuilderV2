using Abstractions.Migration.DDL;
using Application.Features.Migration.Commands;
using MediatR;

public sealed class GenerateDdlHandler
    : IRequestHandler<GenerateDdlCommand, string>
{
    private readonly IGenerateDdlService _service;

    public GenerateDdlHandler(IGenerateDdlService service)
        => _service = service;

    public Task<string> Handle(
        GenerateDdlCommand request,
        CancellationToken cancellationToken)
    { 
        return _service.GenerateDdlScriptsAsync(request);
    }
    
}