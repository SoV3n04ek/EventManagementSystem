using EventManagement.Application.DTOs.EventDtos;
using FluentValidation;

namespace EventManagement.Application.Validators.EventValidators;
public class UpdateEventDtoValidator : AbstractValidator<UpdateEventDto>
{
    public UpdateEventDtoValidator()
    {
        _ = RuleFor(x => x.Name)
            .MinimumLength(3).WithMessage("Event name must be at least 3 characters")
            .MaximumLength(255).WithMessage("Event name cannot exceed 255 characters")
            .When(x => !string.IsNullOrEmpty(x.Name));

        _ = RuleFor(x => x.Description)
            .MinimumLength(10).WithMessage("Description must be at least 10 characters")
            .MaximumLength(2000).WithMessage("Description cannot exceed 2000 characters")
            .When(x => !string.IsNullOrEmpty(x.Description));

        _ = RuleFor(x => x.EventDate)
            .NotEmpty().WithMessage("Event date is required when provided")
            .GreaterThan(DateTime.UtcNow).WithMessage("Event date must be in the future")
            .When(x => x.EventDate.HasValue);

        _ = RuleFor(x => x.Location)
            .MaximumLength(500).WithMessage("Location cannot exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.Location));

        _ = RuleFor(x => x.Capacity)
            .GreaterThan(0).WithMessage("Capacity must be positive")
            .When(x => x.Capacity.HasValue);

        _ = RuleFor(x => x.IsPublic)
            .NotNull().WithMessage("IsPublic field is required")
            .When(x => x.IsPublic.HasValue);
    }
}
