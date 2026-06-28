namespace CatCar.SharedKernel;

/// <summary>
/// Exception thrown when a business rule is violated.
/// </summary>
public class BusinessRuleViolatedException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BusinessRuleViolatedException"/> class.
    /// </summary>
    public BusinessRuleViolatedException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BusinessRuleViolatedException"/> class
    /// with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public BusinessRuleViolatedException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BusinessRuleViolatedException"/> class
    /// with a specified error message and inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public BusinessRuleViolatedException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
