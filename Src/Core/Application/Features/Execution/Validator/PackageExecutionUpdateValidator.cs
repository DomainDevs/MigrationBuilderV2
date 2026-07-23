// PackageExecutionUpdateValidator.cs
using FluentValidation;
using Application.Features.Execution.Commands;

namespace Application.Features.Execution.Validators;

public class PackageExecutionUpdateValidator : AbstractValidator<PackageExecutionUpdateCommand>
{
    public PackageExecutionUpdateValidator()
    {
        RuleFor(x => x.ExecutionId)
            .NotEmpty().WithMessage("El campo ExecutionId es obligatorio.");
        RuleFor(x => x.FileId)
            .NotEmpty().WithMessage("El campo FileId es obligatorio.");
        RuleFor(x => x.FileId)
            .MaximumLength(20).WithMessage("El campo FileId no puede exceder 20 caracteres.");
        RuleFor(x => x.FileName)
            .NotEmpty().WithMessage("El campo FileName es obligatorio.");
        RuleFor(x => x.FileName)
            .MaximumLength(520).WithMessage("El campo FileName no puede exceder 520 caracteres.");
        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("El campo Status es obligatorio.");
        RuleFor(x => x.Status)
            .MaximumLength(40).WithMessage("El campo Status no puede exceder 40 caracteres.");
        RuleFor(x => x.Message)
            .MaximumLength(8000).WithMessage("El campo Message no puede exceder 8000 caracteres.");
    }
}