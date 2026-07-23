// PackageExecutionDeleteCommand.cs
using MediatR;
namespace Application.Features.Execution.Commands;

public record PackageExecutionDeleteCommand(int ExecutionId) : IRequest<bool>;
