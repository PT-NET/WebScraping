using FluentValidation;

namespace WebScraping.Application.Commands.ScreenEntity
{
    public class ScreenEntityCommandValidator : AbstractValidator<ScreenEntityCommand>
    { 
        public ScreenEntityCommandValidator()
        {
            RuleFor(x => x.EntityName)
                .NotEmpty()
                .WithMessage("Entity name is required")
                .MinimumLength(2)
                .WithMessage("Entity name must have at least 2 characters")
                .MaximumLength(200)
                .WithMessage("Entity name cannot exceed 200 characters");

            RuleFor(x => x.Sources)
                .NotEmpty()
                .WithMessage("At least one source must be specified")
                .Must(sources => sources.Count <= 3)
                .WithMessage("Maximum 3 sources allowed");
        }
    }
}
