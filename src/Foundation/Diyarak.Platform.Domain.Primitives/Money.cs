namespace Diyarak.Platform.Domain.Primitives;

public readonly record struct Money : IComparable<Money>
{
    public Money(decimal amount, Currency currency)
    {
        Currency = currency ?? throw new ArgumentNullException(nameof(currency));
        Amount = amount;
    }

    public decimal Amount { get; }
    public Currency Currency { get; }

    public static Money Zero(Currency currency) => new(0m, currency);

    public Money Round(int decimals = 2, MidpointRounding mode = MidpointRounding.ToEven)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(decimals);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(decimals, 28);
        return new Money(decimal.Round(Amount, decimals, mode), Currency);
    }

    public int CompareTo(Money other)
    {
        EnsureSameCurrency(this, other);
        return Amount.CompareTo(other.Amount);
    }

    public static Money operator +(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return new Money(left.Amount + right.Amount, left.Currency);
    }

    public static Money operator -(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return new Money(left.Amount - right.Amount, left.Currency);
    }

    public static bool operator <(Money left, Money right) => left.CompareTo(right) < 0;
    public static bool operator >(Money left, Money right) => left.CompareTo(right) > 0;
    public static bool operator <=(Money left, Money right) => left.CompareTo(right) <= 0;
    public static bool operator >=(Money left, Money right) => left.CompareTo(right) >= 0;

    public static Money operator *(Money money, decimal multiplier) => new(money.Amount * multiplier, money.Currency);
    public static Money operator /(Money money, decimal divisor) => divisor == 0m
        ? throw new DivideByZeroException()
        : new Money(money.Amount / divisor, money.Currency);

    public override string ToString() => $"{Amount} {Currency.Code}";

    private static void EnsureSameCurrency(Money left, Money right)
    {
        if (left.Currency != right.Currency)
            throw new InvalidOperationException($"Currency mismatch: {left.Currency} and {right.Currency}.");
    }
}
