using Application.Features.Project.Commands;
using FluentValidation;

namespace Application.Features.Project.Validators;

public sealed class ProjectDeleteValidator : AbstractValidator<ProjectDeleteCommand>
{
    public ProjectDeleteValidator()
    {
        RuleFor(x=>x.Name).NotEmpty().MaximumLength(100);
    }
}
