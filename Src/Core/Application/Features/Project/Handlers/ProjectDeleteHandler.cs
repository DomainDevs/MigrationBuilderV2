using Application.Features.Project.Commands;
using Infrastructure.System;
using MediatR;

namespace Application.Features.Project.Handlers;

public sealed class ProjectDeleteHandler : IRequestHandler<ProjectDeleteCommand,bool>
{
    private readonly IMigrationProjectInitializer _initializer;

    public ProjectDeleteHandler(IMigrationProjectInitializer initializer) {
        _initializer = initializer;
    }

    public Task<bool> Handle(ProjectDeleteCommand request,CancellationToken ct)
    {
        _initializer.DeleteProject(request.Name);
        return Task.FromResult(true);
    }
        
}
