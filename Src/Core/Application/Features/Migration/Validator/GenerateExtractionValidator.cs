using Application.Features.Migration.Commands;
using FluentValidation;

namespace Application.Features.Migration.Validator;

public sealed class GenerateExtractionValidator
    : AbstractValidator<GenerateExtractionCommand>
{
    public GenerateExtractionValidator()
    {
        RuleFor(x => x.ProjectName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Schema)
            .MaximumLength(4)
            .When(x => !string.IsNullOrWhiteSpace(x.Schema));

        RuleFor(x => x.ArtifactType)
            .IsInEnum();

        RuleFor(x => x.Tables)
            .NotNull();
    }
}
