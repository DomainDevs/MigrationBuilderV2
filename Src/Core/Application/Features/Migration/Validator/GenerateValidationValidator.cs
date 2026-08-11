using Application.Features.Migration.Commands;
using FluentValidation;

namespace Application.Features.Migration.Validators;

public sealed class GenerateValidationValidator
    : AbstractValidator<GenerateValidationCommand>
{
    public GenerateValidationValidator()
    {
        RuleFor(x => x.ProjectName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Schema)
            .MaximumLength(4)
            .When(x => !string.IsNullOrWhiteSpace(x.Schema));

        RuleFor(x => x.Tables)
            .NotNull();
    }
}