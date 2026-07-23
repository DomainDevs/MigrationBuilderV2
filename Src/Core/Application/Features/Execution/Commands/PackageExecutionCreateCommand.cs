// PackageExecutionCreateCommand.cs
using MediatR;
namespace Application.Features.Execution.Commands;

public record PackageExecutionCreateCommand(int ExecutionId, string FileId, string FileName, string Status, DateTime? StartTime, DateTime? EndTime, long? DurationMs, string Message) : IRequest<int>;
