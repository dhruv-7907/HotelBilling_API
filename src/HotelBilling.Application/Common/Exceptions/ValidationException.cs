using FluentValidation.Results;
namespace HotelBilling.Application.Common.Exceptions;
public class ValidationException : Exception
{
    public IDictionary<string, string[]> Errors { get; }
    public ValidationException(IEnumerable<ValidationFailure> failures) : base("One or more validation failures have occurred.")
        => Errors = failures.GroupBy(e => e.PropertyName, e => e.ErrorMessage)
                            .ToDictionary(f => f.Key, f => f.ToArray());
}
