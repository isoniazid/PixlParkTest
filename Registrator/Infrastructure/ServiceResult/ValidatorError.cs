namespace Registrator.Infrastructure.ServiceResult
{
    public class ValidatorError
    {
        public List<KeyValuePair<string,string>> ValidationErrors { get; set; } = new();

        public ValidatorError(FluentValidation.Results.ValidationResult validationResults)
        {
            foreach (var error in validationResults.Errors)
            {
                ValidationErrors.Add(new(error.PropertyName, error.ErrorMessage));
            }

        }
    }
}