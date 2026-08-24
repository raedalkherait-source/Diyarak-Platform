using Diyarak.Platform.Domain.Primitives;
using Diyarak.Platform.SharedKernel;

namespace Diyarak.Platform.Identity;

public sealed record UserEmailChangedDomainEvent : DomainEvent
{
    public UserEmailChangedDomainEvent(Guid userId, EmailAddress email)
    {
        UserId = userId;
        Email = email ?? throw new ArgumentNullException(nameof(email));
    }

    public Guid UserId { get; }

    public EmailAddress Email { get; }
}
