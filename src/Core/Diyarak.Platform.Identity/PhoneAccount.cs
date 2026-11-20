using Diyarak.Platform.Domain.Primitives;

namespace Diyarak.Platform.Identity;

/// <summary>
/// Links a verified phone number to exactly one internal user.
/// </summary>
public sealed record PhoneAccount
{
    public PhoneAccount(
        PhoneNumber phoneNumber,
        Guid userId,
        DateTimeOffset createdAt)
    {
        ArgumentNullException.ThrowIfNull(phoneNumber);

        if (userId == Guid.Empty)
        {
            throw new ArgumentException(
                "User identifier cannot be empty.",
                nameof(userId));
        }

        PhoneNumber = phoneNumber;
        UserId = userId;
        CreatedAt = createdAt;
    }

    public PhoneNumber PhoneNumber { get; }

    public Guid UserId { get; }

    public DateTimeOffset CreatedAt { get; }
}
