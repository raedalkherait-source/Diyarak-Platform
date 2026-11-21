using Diyarak.Platform.SharedKernel;

namespace Diyarak.Market.Company;

public enum ConstructionStatus
{
    Planned = 1,
    UnderConstruction = 2,
    NearCompletion = 3,
}

public sealed class OffPlanProject : AggregateRoot<Guid>
{
    public const int MinimumNameLength = 3;
    public const int MaximumNameLength = 120;
    public const int MaximumDescriptionLength = 4000;
    public const int MaximumDistrictLength = 100;

    private OffPlanProject(Guid id, Guid agencyId, long version)
        : base(id)
    {
        if (agencyId == Guid.Empty)
            throw new ArgumentException("A project needs an agency.", nameof(agencyId));

        AgencyId = agencyId;
        Version = version;
        Name = string.Empty;
        Governorate = string.Empty;
        District = string.Empty;
    }

    public Guid AgencyId { get; }

    public long Version { get; }

    public string Name { get; private set; }

    public string? Description { get; private set; }

    public string Governorate { get; private set; }

    public string District { get; private set; }

    public ConstructionStatus ConstructionStatus { get; private set; }

    public int TotalUnits { get; private set; }

    public decimal? MinPriceUsd { get; private set; }

    public decimal? MaxPriceUsd { get; private set; }

    public DateOnly? ExpectedCompletion { get; private set; }

    public bool IsPublished { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static OffPlanProject Create(
        Guid id,
        Guid agencyId,
        string name,
        string? description,
        string governorate,
        string district,
        ConstructionStatus constructionStatus,
        int totalUnits,
        decimal? minPriceUsd,
        decimal? maxPriceUsd,
        DateOnly? expectedCompletion,
        DateTimeOffset now)
    {
        var project = new OffPlanProject(id, agencyId, 1) { CreatedAt = now };
        project.SetDetails(name, description, governorate, district, constructionStatus, totalUnits, minPriceUsd, maxPriceUsd, expectedCompletion);
        return project;
    }

    public static OffPlanProject Restore(
        Guid id,
        Guid agencyId,
        long version,
        string name,
        string? description,
        string governorate,
        string district,
        ConstructionStatus constructionStatus,
        int totalUnits,
        decimal? minPriceUsd,
        decimal? maxPriceUsd,
        DateOnly? expectedCompletion,
        bool isPublished,
        DateTimeOffset createdAt)
    {
        var project = new OffPlanProject(id, agencyId, version)
        {
            IsPublished = isPublished,
            CreatedAt = createdAt,
        };
        project.SetDetailsUnchecked(name, description, governorate, district, constructionStatus, totalUnits, minPriceUsd, maxPriceUsd, expectedCompletion);
        return project;
    }

    public void Update(
        string name,
        string? description,
        string governorate,
        string district,
        ConstructionStatus constructionStatus,
        int totalUnits,
        decimal? minPriceUsd,
        decimal? maxPriceUsd,
        DateOnly? expectedCompletion)
    {
        SetDetails(name, description, governorate, district, constructionStatus, totalUnits, minPriceUsd, maxPriceUsd, expectedCompletion);
    }

    public void Publish()
    {
        if (IsPublished)
            throw new InvalidOperationException("The project is already published.");

        IsPublished = true;
    }

    public void Unpublish()
    {
        if (!IsPublished)
            throw new InvalidOperationException("The project is not published.");

        IsPublished = false;
    }

    private void SetDetails(
        string name,
        string? description,
        string governorate,
        string district,
        ConstructionStatus constructionStatus,
        int totalUnits,
        decimal? minPriceUsd,
        decimal? maxPriceUsd,
        DateOnly? expectedCompletion)
    {
        if (!Enum.IsDefined(constructionStatus))
            throw new ArgumentOutOfRangeException(nameof(constructionStatus));

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(totalUnits);

        if (minPriceUsd is not null && minPriceUsd <= 0)
            throw new ArgumentOutOfRangeException(nameof(minPriceUsd));
        if (maxPriceUsd is not null && maxPriceUsd <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxPriceUsd));
        if (minPriceUsd is not null && maxPriceUsd is not null && minPriceUsd > maxPriceUsd)
            throw new ArgumentException("Minimum price cannot exceed maximum price.", nameof(minPriceUsd));

        SetDetailsUnchecked(name, description, governorate, district, constructionStatus, totalUnits, minPriceUsd, maxPriceUsd, expectedCompletion);
    }

    private void SetDetailsUnchecked(
        string name,
        string? description,
        string governorate,
        string district,
        ConstructionStatus constructionStatus,
        int totalUnits,
        decimal? minPriceUsd,
        decimal? maxPriceUsd,
        DateOnly? expectedCompletion)
    {
        Name = Required(name, MaximumNameLength, nameof(name), MinimumNameLength);
        Description = Optional(description, MaximumDescriptionLength, nameof(description), allowLineBreaks: true);
        Governorate = Required(governorate, 40, nameof(governorate));
        District = Required(district, MaximumDistrictLength, nameof(district));
        ConstructionStatus = constructionStatus;
        TotalUnits = totalUnits;
        MinPriceUsd = minPriceUsd;
        MaxPriceUsd = maxPriceUsd;
        ExpectedCompletion = expectedCompletion;
    }

    private static string Required(string value, int maximum, string parameter, int minimum = 1) =>
        Optional(value, maximum, parameter) is { Length: var len } trimmed && len >= minimum
            ? trimmed
            : throw new ArgumentException($"A value of at least {minimum} characters is required.", parameter);

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
