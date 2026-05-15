using FluentValidation;
using Trickler_API.DTO;

namespace Trickler_API.Validators
{
    public class SubmitAttemptRequestValidator : AbstractValidator<SubmitAttemptRequest>
    {
        public SubmitAttemptRequestValidator()
        {
            RuleFor(x => x.Answer)
                .NotEmpty().WithMessage("Answer is required");
        }
    }
}
