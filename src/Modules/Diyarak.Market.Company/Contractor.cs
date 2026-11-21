using Diyarak.Platform.SharedKernel;

namespace Diyarak.Market.Company;

public enum ContractorSpecialty
{
    GeneralConstruction = 1,
    InteriorDesign = 2,
    Plumbing = 3,
    Electrical = 4,
    Painting = 5,
    Tiling = 6,
    Carpentry = 7,
    Landscaping = 8,
    Demolition = 9,
    Surveying = 10,
}

public sealed class Contractor : AggregateRoot<Guid>
{
    public const int MinimumNameLength = 2;
    public const int MaximumNameLength = 100;
    public const int MaximumDescriptionLength = 2000;
    public const int MaximumPhoneLength = 20;

    private Contractor(Guid id, Guid ownerUserId, long version)
        : base(id)
    {
        if (ownerUserId == Guid.Empty)
            throw new ArgumentException("A contractor needs an owner.", nameof(ownerUserId));

        OwnerUserId = ownerUserId;
        Version = version;
        Name = string.Empty;
        Governorate = string.Empty;
        Phone = string.Empty;
    }

    public Guid OwnerUserId { get; }

    public long Version { get; }

    public string Name { get; private set; }

    public string? Description { get; private set; }

    public string Governorate { get; private set; }

    public string? District { get; private set; }

    public ContractorSpecialty Specialty { get; private set; }

    public string Phone { get; private set; }

    public bool IsPublished { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static Contractor Create(
        Guid id,
        Guid ownerUserId,
        string name,
        string? description,
        string governorate,
        string? district,
        ContractorSpecialty specialty,
        string phone,
        DateTimeOffset createdAt)
    {
        var contractor = new Contractor(id, ownerUserId, 1) { CreatedAt = createdAt };
        contractor.SetDetails(name, description, governorate, district, specialty, phone);
        return contractor;
    }

    public static Contractor Restore(
        Guid id,
        Guid ownerUserId,
        long version,
        string name,
        string? description,
        string governorate,
        string? district,
        ContractorSpecialty specialty,
        string phone,
        bool isPublished,
        DateTimeOffset createdAt)
    {
        var contractor = new Contractor(id, ownerUserId, version)
        {
            CreatedAt = createdAt,
            IsPublished = isPublished,
        };
        contractor.SetDetailsUnchecked(name, description, governorate, district, specialty, phone);
        return contractor;
    }

    public void Update(
        string name,
        string? description,
        string governorate,
        string? district,
        ContractorSpecialty specialty,
        string phone)
    {
        SetDetails(name, description, governorate, district, specialty, phone);
    }

    public void Publish() => IsPublished = true;

    public void Unpublish() => IsPublished = false;

    private void SetDetails(
        string name,
        string? description,
        string governorate,
        string? district,
        ContractorSpecialty specialty,
        string phone)
    {
        if (!Enum.IsDefined(specialty))
            throw new ArgumentException("Invalid specialty.", nameof(specialty));

        SetDetailsUnchecked(name, description, governorate, district, specialty, phone);
    }

    private void SetDetailsUnchecked(
        string name,
        string? description,
        string governorate,
        string? district,
        ContractorSpecialty specialty,
        string phone)
    {
        string trimmedName = name?.Trim() ?? throw new ArgumentNullException(nameof(name));
        if (trimmedName.Length < MinimumNameLength || trimmedName.Length > MaximumNameLength)
            throw new ArgumentException($"Name must be {MinimumNameLength}-{MaximumNameLength} characters.", nameof(name));
        if (trimmedName.Any(char.IsControl))
            throw new ArgumentException("Name contains control characters.", nameof(name));

        string trimmedPhone = phone?.Trim() ?? throw new ArgumentNullException(nameof(phone));
        if (trimmedPhone.Length is 0 or > MaximumPhoneLength)
            throw new ArgumentException("Phone is required and must be at most 20 characters.", nameof(phone));

        string trimmedGov = governorate?.Trim() ?? throw new ArgumentNullException(nameof(governorate));
        if (trimmedGov.Length == 0)
            throw new ArgumentException("Governorate is required.", nameof(governorate));

        string? trimmedDescription = description?.Trim();
        if (trimmedDescription is { Length: > MaximumDescriptionLength })
            throw new ArgumentException($"Description exceeds {MaximumDescriptionLength} characters.", nameof(description));
        if (trimmedDescription is { Length: > 0 } && trimmedDescription.Any(c => char.IsControl(c) && c is not '\n' and not '\r'))
            throw new ArgumentException("Description contains control characters.", nameof(description));

        Name = trimmedName;
        Description = string.IsNullOrEmpty(trimmedDescription) ? null : trimmedDescription;
        Governorate = trimmedGov;
        District = district?.Trim() is { Length: > 0 } d ? d : null;
        Specialty = specialty;
        Phone = trimmedPhone;
    }
}
