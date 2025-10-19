using EventManagement.Application.DTOs.ParticipantDtos;
using FluentValidation;

public class ParticipantDtoValidator : AbstractValidator<ParticipantDto>
{
    public ParticipantDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Participant name is required")
            .MaximumLength(255).WithMessage("Participant name cannot exceed 255 characters")
            .MinimumLength(2).WithMessage("Participant name must be at least 2 characters");
    }
}