using MediatR;

namespace Application.Features.Project.Commands;

public record ProjectCreateCommand(string Name) : IRequest<bool>;
