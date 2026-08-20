namespace Diyarak.Platform.Domain.Primitives.Tests;

public sealed class ContactAndLocationTests
{
    [Theory]
    [InlineData(90, 180)]
    [InlineData(-90, -180)]
    [InlineData(33.5138, 36.2765)]
    public void Coordinates_accept_boundaries(double latitude, double longitude) => Assert.Equal(latitude, new GeoCoordinate(latitude, longitude).Latitude);
    [Theory]
    [InlineData(90.1, 0)]
    [InlineData(0, 180.1)]
    [InlineData(-91, 0)]
    public void Coordinates_reject_out_of_range(double latitude, double longitude) => Assert.Throws<ArgumentOutOfRangeException>(() => new GeoCoordinate(latitude, longitude));
    [Fact] public void Email_is_normalized() => Assert.Equal("person@example.com", EmailAddress.Create(" Person@Example.COM ").Value);
    [Theory]
    [InlineData("bad")]
    [InlineData("a@")]
    public void Email_rejects_invalid_values(string value) => Assert.Throws<ArgumentException>(() => EmailAddress.Create(value));
    [Theory]
    [InlineData("+963944123456")]
    [InlineData("+4915112345678")]
    public void Phone_accepts_e164(string value) => Assert.Equal(value, PhoneNumber.Create(value).Value);
    [Theory]
    [InlineData("0944123456")]
    [InlineData("+012345678")]
    public void Phone_rejects_non_e164(string value) => Assert.Throws<ArgumentException>(() => PhoneNumber.Create(value));
}
