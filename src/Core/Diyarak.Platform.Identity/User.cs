using Diyarak.Platform.Domain.Primitives;
using Diyarak.Platform.SharedKernel;

namespace Diyarak.Platform.Identity;

public sealed class User : AggregateRoot<Guid>
{
    public User(Guid id, EmailAddress email)
        : base(id)
    {
        Email = email ?? throw new ArgumentNullException(nameof(email));
    }

    public EmailAddress Email { get; private set; }

    public void ChangeEmail(EmailAddress email)
    {
        ArgumentNullException.ThrowIfNull(email);

        if (Email == email)
            return;

        Email = email;
        RaiseDomainEvent(new UserEmailChangedDomainEvent(Id, Email));
    }
}
