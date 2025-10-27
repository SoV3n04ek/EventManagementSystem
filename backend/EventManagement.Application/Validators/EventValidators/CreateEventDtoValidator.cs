using EventManagement.Application.DTOs.EventDtos;
using FluentValidation;

public class CreateEventDtoValidator : AbstractValidator<CreateEventDto>
{
    public CreateEventDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Event name is required")
            .MinimumLength(3).WithMessage("Event name must be at least 3 characters")
            .MaximumLength(255).WithMessage("Event name cannot exceed 255 characters");

        RuleFor(x => x.Description)
            .MinimumLength(10).WithMessage("Description must be at least 10 characters")
            .MaximumLength(2000).WithMessage("Description cannot exceed 2000 characters")
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.EventDate)
            .NotEmpty().WithMessage("Event date is required")
            .GreaterThan(DateTime.UtcNow).WithMessage("Event date must be in the future");

        RuleFor(x => x.Location)
            .NotEmpty().WithMessage("Location is required")
            .MinimumLength(2).WithMessage("Location must be at least 2 characters")
            .MaximumLength(500).WithMessage("Location cannot exceed 500 characters");

        RuleFor(x => x.Capacity)
            .GreaterThan(0).WithMessage("Capacity must be positive")
            .When(x => x.Capacity.HasValue);

        RuleFor(x => x.IsPublic)
            .NotNull().WithMessage("IsPublic field is required");
    }
}