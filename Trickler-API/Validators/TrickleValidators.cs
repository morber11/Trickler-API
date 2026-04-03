using FluentValidation;
using Trickler_API.Constants;
using Trickler_API.DTO;
using Trickler_API.Models;

namespace Trickler_API.Validators
{
    public class CreateTrickleRequestValidator : AbstractValidator<CreateTrickleRequest>
    {
        public CreateTrickleRequestValidator(IValidator<AvailabilityDto> availabilityValidator)
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage(MessageConstants.Validation.TitleRequired)
                .MaximumLength(1000).WithMessage(MessageConstants.Validation.TitleMaxLength);

            RuleFor(x => x.Text)
                .NotEmpty().WithMessage(MessageConstants.Validation.TextRequired)
                .MaximumLength(1000).WithMessage(MessageConstants.Validation.TextMaxLength);

            RuleFor(x => x.Answers)
                .Must(answers => answers is null || answers.All(a => a is not null && !string.IsNullOrWhiteSpace(a.Answer)))
                .WithMessage(MessageConstants.Validation.AnswersCannotContainEmptyValues);

            RuleFor(x => x.Availability!)
                .SetValidator(availabilityValidator)
                .When(x => x.Availability is not null);

            RuleFor(x => x.Score)
                .NotNull().WithMessage(MessageConstants.Validation.ScoreRequired)
                .GreaterThanOrEqualTo(0);

            RuleFor(x => x.AttemptsPerTrickle)
                .Must(v => v >= -1)
                .WithMessage("AttemptsPerTrickle must be -1 (unlimited) or >= 1");

            RuleFor(x => x.RewardText)
                .NotNull().WithMessage(MessageConstants.Validation.RewardTextRequired)
                .NotEmpty().WithMessage(MessageConstants.Validation.RewardTextRequired)
                .MaximumLength(1000);
        }
    }

    public class UpdateTrickleRequestValidator : AbstractValidator<UpdateTrickleRequest>
    {
        public UpdateTrickleRequestValidator(IValidator<AvailabilityDto> availabilityValidator)
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage(MessageConstants.Validation.TitleRequired)
                .MaximumLength(1000).WithMessage(MessageConstants.Validation.TitleMaxLength);

            RuleFor(x => x.Text)
                .NotEmpty().WithMessage(MessageConstants.Validation.TextRequired)
                .MaximumLength(1000).WithMessage(MessageConstants.Validation.TextMaxLength);

            RuleFor(x => x.Answers)
                .Must(answers => answers is null || answers.All(a => a is not null && !string.IsNullOrWhiteSpace(a.Answer)))
                .WithMessage(MessageConstants.Validation.AnswersCannotContainEmptyValues);

            RuleFor(x => x.Availability!)
                .SetValidator(availabilityValidator)
                .When(x => x.Availability is not null);

            RuleFor(x => x.Score)
                .NotNull().WithMessage(MessageConstants.Validation.ScoreRequired)
                .GreaterThanOrEqualTo(0);

            RuleFor(x => x.AttemptsPerTrickle)
                .Must(v => v >= -1)
                .WithMessage("AttemptsPerTrickle must be -1 (unlimited) or >= 1");

            RuleFor(x => x.RewardText)
                .NotNull().WithMessage(MessageConstants.Validation.RewardTextRequired)
                .NotEmpty().WithMessage(MessageConstants.Validation.RewardTextRequired)
                .MaximumLength(1000);
        }
    }

    public class AvailabilityDtoValidator : AbstractValidator<AvailabilityDto>
    {
        public AvailabilityDtoValidator()
        {
            RuleFor(x => x.Type)
                .NotEmpty().WithMessage(MessageConstants.Validation.AvailabilityTypeRequired)
                .Must(type => Enum.TryParse<AvailabilityType>(type, true, out _))
                .WithMessage(MessageConstants.Validation.InvalidAvailabilityType);

            RuleFor(x => x.From)
                .LessThanOrEqualTo(x => x.Until)
                .When(x => x.From.HasValue && x.Until.HasValue)
                .WithMessage(MessageConstants.Validation.FromDateBeforeUntil);

            RuleFor(x => x.Dates)
                .NotEmpty()
                .When(x => Enum.TryParse<AvailabilityType>(x.Type, true, out var t) && t == AvailabilityType.SpecificDates)
                .WithMessage(MessageConstants.Validation.DatesRequired);

            RuleFor(x => x.DaysOfWeek)
                .NotEmpty()
                .When(x => Enum.TryParse<AvailabilityType>(x.Type, true, out var t) && t == AvailabilityType.Weekly)
                .WithMessage(MessageConstants.Validation.DaysOfWeekRequired);
        }
    }
}
