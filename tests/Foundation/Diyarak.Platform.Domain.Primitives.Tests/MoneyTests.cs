namespace Diyarak.Platform.Domain.Primitives.Tests;

public sealed class MoneyTests
{
    [Fact] public void Constructor_preserves_amount_and_currency() { Money value = new(12.5m, Currency.Syp); Assert.Equal(12.5m, value.Amount); Assert.Equal(Currency.Syp, value.Currency); }
    [Fact] public void Add_same_currency() => Assert.Equal(new Money(15m, Currency.Syp), new Money(10m, Currency.Syp) + new Money(5m, Currency.Syp));
    [Fact] public void Subtract_same_currency() => Assert.Equal(new Money(5m, Currency.Syp), new Money(10m, Currency.Syp) - new Money(5m, Currency.Syp));
    [Fact] public void Add_different_currency_throws() => Assert.Throws<InvalidOperationException>(() => _ = new Money(1m, Currency.Syp) + new Money(1m, Currency.Usd));
    [Fact] public void Divide_by_zero_throws() => Assert.Throws<DivideByZeroException>(() => _ = new Money(1m, Currency.Syp) / 0m);
    [Fact] public void Round_uses_requested_precision() => Assert.Equal(1.24m, new Money(1.235m, Currency.Syp).Round(2, MidpointRounding.AwayFromZero).Amount);
    [Fact] public void Negative_values_are_supported_for_accounting_semantics() => Assert.Equal(-1m, new Money(-1m, Currency.Syp).Amount);
}
