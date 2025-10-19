using EventManagement.Application.DTOs.UserDtos;
using FluentValidation;

public class LoginDtoValidator : AbstractValidator<LoginDto>
{
    public LoginDtoValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Valid email address is required")
            .MaximumLength(254).WithMessage("Email cannot exceed 254 characters");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")
            .MaximumLength(64).WithMessage("Password cannot exceed 64 characters");
    }
}