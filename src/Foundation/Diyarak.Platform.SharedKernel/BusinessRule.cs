namespace Diyarak.Platform.SharedKernel;

public static class BusinessRule
{
    public static void Check(IBusinessRule rule)
    {
        ArgumentNullException.ThrowIfNull(rule);
        if (rule.IsBroken()) throw new BusinessRuleValidationException(rule);
    }
}
