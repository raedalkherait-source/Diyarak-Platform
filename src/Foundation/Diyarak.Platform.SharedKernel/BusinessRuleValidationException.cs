namespace Diyarak.Platform.SharedKernel;

public sealed class BusinessRuleValidationException : Exception
{
    public BusinessRuleValidationException(IBusinessRule rule) : base(rule?.Message)
    {
        Rule = rule ?? throw new ArgumentNullException(nameof(rule));
    }
    public IBusinessRule Rule { get; }
}
