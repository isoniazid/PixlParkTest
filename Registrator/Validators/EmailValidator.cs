using FluentValidation;

namespace Registrator.Validators
{
    public class EmailValidator : AbstractValidator<string>
    {
        public EmailValidator()
        {
            RuleFor(email => email)
                .NotEmpty().WithMessage("Email не может быть пустым")
                .EmailAddress().WithMessage("Некорректный формат email");
        }
    }
}