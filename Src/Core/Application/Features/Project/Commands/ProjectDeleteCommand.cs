using MediatR;

namespace Application.Features.Project.Commands;

public record ProjectDeleteCommand(string Name) : IRequest<bool>;
