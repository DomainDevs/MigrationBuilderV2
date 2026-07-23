// PackageExecutionDeleteValidator.cs
using FluentValidation;
using Application.Features.Execution.Commands;

namespace Application.Features.Execution.Validators;

public class PackageExecutionDeleteValidator : AbstractValidator<PackageExecutionDeleteCommand>
{
    public PackageExecutionDeleteValidator()
    {
        RuleFor(x => x.ExecutionId)
            .GreaterThan(0).WithMessage("El identificador ExecutionId debe ser mayor que cero.");
    }
}