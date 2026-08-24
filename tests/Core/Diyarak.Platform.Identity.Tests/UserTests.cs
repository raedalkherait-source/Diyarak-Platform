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
}
