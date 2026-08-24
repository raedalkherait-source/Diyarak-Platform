using Diyarak.Platform.Domain.Primitives;
using Xunit;

namespace Diyarak.Platform.Identity.Tests;

public sealed class UserTests
{
    [Fact]
    public void Constructor_sets_id_and_email()
    {
        Guid id = Guid.NewGuid();
        EmailAddress email = EmailAddress.Create("User@Example.com");

        User user = new(id, email);

        Assert.Equal(id, user.Id);
        Assert.Equal(email, user.Email);
    }

    [Fact]
    public void Change_email_updates_email()
    {
        User user = new(Guid.NewGuid(), EmailAddress.Create("old@example.com"));
        EmailAddress newEmail = EmailAddress.Create("new@example.com");

        user.ChangeEmail(newEmail);

        Assert.Equal(newEmail, user.Email);
    }

    [Fact]
    public void Change_email_raises_domain_event()
    {
        Guid id = Guid.NewGuid();
        User user = new(id, EmailAddress.Create("old@example.com"));
        EmailAddress newEmail = EmailAddress.Create("new@example.com");

        user.ChangeEmail(newEmail);

        UserEmailChangedDomainEvent domainEvent =
            Assert.IsType<UserEmailChangedDomainEvent>(Assert.Single(user.DomainEvents));

        Assert.Equal(id, domainEvent.UserId);
        Assert.Equal(newEmail, domainEvent.Email);
    }

    [Fact]
    public void Change_email_to_same_email_does_not_raise_domain_event()
    {
        EmailAddress email = EmailAddress.Create("user@example.com");
        User user = new(Guid.NewGuid(), email);

        user.ChangeEmail(email);

        Assert.Empty(user.DomainEvents);
    }
}
