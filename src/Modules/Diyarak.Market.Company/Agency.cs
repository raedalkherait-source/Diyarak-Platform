using Diyarak.Platform.SharedKernel;

namespace Diyarak.Market.Company;

/// <summary>
/// A real-estate agency (مكتب عقاري) owned by one user. Its verification badge is granted by
/// moderators and is lost when the agency is renamed, so a verified name cannot be reused for
/// another identity (ADR-0068).
/// </summary>
public sealed class Agency : AggregateRoot<Guid>
{
    public const int MinimumNameLength = 2;
    public const int MaximumNameLength = 80;
    public const int MaximumDescriptionLength = 1000;
    public const int MaximumCityLength = 100;
    public const int MaximumRegistrationLength = 60;
    public const int MaximumNoteLength = 500;

    private Agency(Guid id, Guid ownerUserId, long version)
        : base(id)
    {
        if (ownerUserId == Guid.Empty)
            throw new ArgumentException("An agency needs an owner.", nameof(ownerUserId));

        OwnerUserId = ownerUserId;
        Version = version;
        Name = string.Empty;
        City = string.Empty;
    }

    public Guid OwnerUserId { get; }

    public long Version { get; }

    public string Name { get; private set; }

    public string? Description { get; private set; }

    /// <summary>Governorate code (validated by the application layer against the Syrian list).</summary>
    public string? Governorate { get; private set; }

    public string City { get; private set; }

    public AgencyVerificationStatus VerificationStatus { get; private set; }

    /// <summary>Commercial registration or licence number submitted with the request.</summary>
    public string? RegistrationNumber { get; private set; }

    /// <summary>Moderator note on rejection.</summary>
    public string? VerificationNote { get; private set; }

    public DateTimeOffset? VerifiedAt { get; private set; }

    public bool IsVerified => VerificationStatus == AgencyVerificationStatus.Verified;

    public static Agency Create(Guid id, Guid ownerUserId, string name, string? description, string? governorate, string city)
    {
        var agency = new Agency(id, ownerUserId, 1);
        agency.UpdateProfile(name, description, governorate, city);
        return agency;
    }

    /// <summary>Rebuilds a stored agency; persistence only.</summary>
    public static Agency Restore(
        Guid id,
        Guid ownerUserId,
        long version,
        string name,
        string? description,
        string? governorate,
        string city,
        AgencyVerificationStatus status,
        string? registrationNumber,
        string? verificationNote,
        DateTimeOffset? verifiedAt)
    {
        var agency = new Agency(id, ownerUserId, version);
        agency.SetProfile(name, description, governorate, city);
        if (!Enum.IsDefined(status))
            throw new InvalidOperationException("Stored agency verification status is unsupported.");
        agency.VerificationStatus = status;
        agency.RegistrationNumber = registrationNumber;
        agency.VerificationNote = verificationNote;
        agency.VerifiedAt = verifiedAt;
        return agency;
    }

    public void UpdateProfile(string name, string? description, string? governorate, string city)
    {
        string previousName = Name;
        SetProfile(name, description, governorate, city);

        bool renamed = previousName.Length > 0 && !string.Equals(previousName, Name, StringComparison.Ordinal);
        if (renamed && VerificationStatus is AgencyVerificationStatus.Verified or AgencyVerificationStatus.Pending)
        {
            VerificationStatus = AgencyVerificationStatus.None;
            VerifiedAt = null;
            VerificationNote = null;
        }
    }

    public void RequestVerification(string registrationNumber)
    {
        if (VerificationStatus is not (AgencyVerificationStatus.None or AgencyVerificationStatus.Rejected))
            throw new InvalidOperationException("Verification can be requested only when none is pending or granted.");

        string trimmed = Required(registrationNumber, MaximumRegistrationLength, nameof(registrationNumber));
        RegistrationNumber = trimmed;
        VerificationStatus = AgencyVerificationStatus.Pending;
        VerificationNote = null;
    }

    public void Verify(DateTimeOffset at)
    {
        if (VerificationStatus != AgencyVerificationStatus.Pending)
            throw new InvalidOperationException("Only a pending agency can be verified.");

        VerificationStatus = AgencyVerificationStatus.Verified;
        VerifiedAt = at;
        VerificationNote = null;
    }

    public void Reject(string note)
    {
        if (VerificationStatus != AgencyVerificationStatus.Pending)
            throw new InvalidOperationException("Only a pending agency can be rejected.");

        VerificationNote = Required(note, MaximumNoteLength, nameof(note));
        VerificationStatus = AgencyVerificationStatus.Rejected;
    }

    private void SetProfile(string name, string? description, string? governorate, string city)
    {
        string trimmedName = Required(name, MaximumNameLength, nameof(name));
        if (trimmedName.Length < MinimumNameLength)
            throw new ArgumentException("The agency name is too short.", nameof(name));

        Name = trimmedName;
        Description = Optional(description, MaximumDescriptionLength, nameof(description), allowLineBreaks: true);
        Governorate = Optional(governorate, 40, nameof(governorate));
        City = Required(city, MaximumCityLength, nameof(city));
    }

    private static string Required(string value, int maximum, string parameter) =>
        Optional(value, maximum, parameter) ?? throw new ArgumentException("A value is required.", parameter);

    private static string? Optional(string? value, int maximum, string parameter, bool allowLineBreaks = false)
    {
        string? trimmed = value?.Trim();
        if (string.IsNullOrEmpty(trimmed))
            return null;

        if (trimmed.Length > maximum)
            throw new ArgumentException($"The value exceeds {maximum} characters.", parameter);

        if (trimmed.Any(character => char.IsControl(character) && !(allowLineBreaks && character is '\n' or '\r')))
            throw new ArgumentException("The value contains control characters.", parameter);

        return trimmed;
    }
}
