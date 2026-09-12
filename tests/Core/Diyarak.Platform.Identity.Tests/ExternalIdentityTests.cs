using Xunit;

namespace Diyarak.Platform.Identity.Tests;

public sealed class ExternalIdentityTests
{
    [Fact]
    public void Constructor_preserves_issuer_and_subject_exactly()
    {
        const string issuer = "https://IDP.example.test/tenant/";
        const string subject = "Subject-ABC-123";

        var identity = new ExternalIdentity(
            issuer,
            subject);

        Assert.Equal(issuer, identity.Issuer);
        Assert.Equal(subject, identity.Subject);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_rejects_empty_issuer(
        string issuer)
    {
        Assert.Throws<ArgumentException>(
            () => new ExternalIdentity(
                issuer,
                "subject"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_rejects_empty_subject(
        string subject)
    {
        Assert.Throws<ArgumentException>(
            () => new ExternalIdentity(
                "https://idp.example.test/",
                subject));
    }
}
