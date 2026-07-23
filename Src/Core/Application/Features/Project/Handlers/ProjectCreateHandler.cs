//using Application.Abstractions.Projects;
using Application.Features.Project.Commands;
using Infrastructure.System;
using MediatR;

namespace Application.Features.Project.Handlers;

public class ProjectCreateHandler : IRequestHandler<ProjectCreateCommand,bool>
{
    private readonly IMigrationProjectInitializer _initializer;

    public ProjectCreateHandler(IMigrationProjectInitializer initializer) {
        _initializer = initializer;
    } 

    public Task<bool> Handle(ProjectCreateCommand request, CancellationToken ct)
    {
        _initializer.CreateProject(request.Name);
        return Task.FromResult(true);
    }
}
