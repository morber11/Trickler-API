using FluentValidation;
using Trickler_API.DTO;
using Trickler_API.Models;

namespace Trickler_API.Validators
{
    public class CreateTrickleRequestValidator : AbstractValidator<CreateTrickleRequest>
    {
        public CreateTrickleRequestValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Title is required")
                .MaximumLength(1000).WithMessage("Title cannot exceed 1000 characters");

            RuleFor(x => x.Text)
                .NotEmpty().WithMessage("Text is required")
                .MaximumLength(1000).WithMessage("Text cannot exceed 1000 characters");

            RuleFor(x => x.Answers)
                .Must(answers => answers is null || answers.All(a => a is not null && !string.IsNullOrWhiteSpace(a.Answer)))
                .WithMessage("Answers cannot contain empty values");

            RuleFor(x => x.Availability)
                .SetValidator(new AvailabilityDtoValidator()!)
                .When(x => x.Availability is not null);
        }
    }

    public class UpdateTrickleRequestValidator : AbstractValidator<UpdateTrickleRequest>
    {
        public UpdateTrickleRequestValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Title is required")
                .MaximumLength(1000).WithMessage("Title cannot exceed 1000 characters");

            RuleFor(x => x.Text)
                .NotEmpty().WithMessage("Text is required")
                .MaximumLength(1000).WithMessage("Text cannot exceed 1000 characters");

            RuleFor(x => x.Answers)
                .Must(answers => answers is null || answers.All(a => a is not null && !string.IsNullOrWhiteSpace(a.Answer)))
                .WithMessage("Answers cannot contain empty values");

            RuleFor(x => x.Availability)
                .SetValidator(new AvailabilityDtoValidator()!)
                .When(x => x.Availability is not null);
        }
    }

    public class AvailabilityDtoValidator : AbstractValidator<AvailabilityDto>
    {
        public AvailabilityDtoValidator()
        {
            RuleFor(x => x.Type)
                .NotEmpty().WithMessage("Type is required")
                .Must(type => Enum.TryParse<AvailabilityType>(type, true, out _))
                .WithMessage("Invalid availability type");

            RuleFor(x => x.From)
                .LessThanOrEqualTo(x => x.Until)
                .When(x => x.From.HasValue && x.Until.HasValue)
                .WithMessage("From date must be before or equal to Until date");

            RuleFor(x => x.Dates)
                .NotEmpty()
                .When(x => Enum.TryParse<AvailabilityType>(x.Type, true, out var t) && t == AvailabilityType.SpecificDates)
                .WithMessage("Dates are required for SpecificDates availability type");

            RuleFor(x => x.DaysOfWeek)
                .NotEmpty()
                .When(x => Enum.TryParse<AvailabilityType>(x.Type, true, out var t) && t == AvailabilityType.Weekly)
                .WithMessage("Days of week are required for Weekly availability type");
        }
    }
}
