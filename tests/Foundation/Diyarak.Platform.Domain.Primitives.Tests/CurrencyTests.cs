namespace Diyarak.Platform.Domain.Primitives.Tests;

public sealed class CurrencyTests
{
    [Theory]
    [InlineData("syp", "SYP")]
    [InlineData(" USD ", "USD")]
    [InlineData("eur", "EUR")]
    public void Create_normalizes_valid_codes(string input, string expected) => Assert.Equal(expected, Currency.Create(input).Code);
    [Theory]
    [InlineData("")]
    [InlineData("US")]
    [InlineData("USDD")]
    [InlineData("U1D")]
    public void Create_rejects_invalid_codes(string input) => Assert.ThrowsAny<ArgumentException>(() => Currency.Create(input));
    [Fact] public void Equal_codes_are_equal() => Assert.Equal(Currency.Create("SYP"), Currency.Create("syp"));
    [Fact] public void Static_syp_is_available() => Assert.Equal("SYP", Currency.Syp.Code);
}
