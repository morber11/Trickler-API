using FluentValidation;
using Trickler_API.DTO;

namespace Trickler_API.Validators
{
    public class VerifyAnswerRequestValidator : AbstractValidator<VerifyAnswerRequest>
    {
        public VerifyAnswerRequestValidator()
        {
            RuleFor(x => x.TrickleId)
                .GreaterThan(0).WithMessage("Trickle ID must be greater than 0");

            RuleFor(x => x.Answer)
                .NotEmpty().WithMessage("Answer is required");
        }
    }

    public class SubmitAnswerRequestValidator : AbstractValidator<SubmitAnswerRequest>
    {
        public SubmitAnswerRequestValidator()
        {
            RuleFor(x => x.TrickleId)
                .GreaterThan(0).WithMessage("Trickle ID must be greater than 0");

            RuleFor(x => x.Answer)
                .NotEmpty().WithMessage("Answer is required");
        }
    }
}
