namespace Diyarak.Platform.Domain.Primitives.Tests;

public sealed class CodeAndPercentageTests
{
    [Theory][InlineData(0)][InlineData(50)][InlineData(100)] public void Percentage_accepts_range(decimal value) => Assert.Equal(value, new Percentage(value).Value);
    [Theory][InlineData(-0.1)][InlineData(100.1)] public void Percentage_rejects_outside_range(decimal value) => Assert.Throws<ArgumentOutOfRangeException>(() => new Percentage(value));
    [Fact] public void Percentage_fraction_is_calculated() => Assert.Equal(0.125m, new Percentage(12.5m).AsFraction);
    [Theory]
    [InlineData("ar")]
    [InlineData("ar-SY")]
    [InlineData("en-US")]
    public void Language_code_accepts_common_forms(string value) => Assert.Equal(value, LanguageCode.Create(value).Value);
    [Fact] public void Country_code_is_uppercase() => Assert.Equal("SY", CountryCode.Create("sy").Value);
}
