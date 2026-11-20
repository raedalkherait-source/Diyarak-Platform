using Diyarak.Platform.SharedKernel;
using PlatformListing = Diyarak.Platform.Listing;

namespace Diyarak.Market.Listing;

public sealed class Listing : AggregateRoot<Guid>
{
    public const long InitialVersion = 1;

    public Listing(
        Guid id,
        Guid publisherUserId,
        PlatformListing.ListingSubjectReference subjectReference)
        : this(id, publisherUserId, subjectReference, version: InitialVersion)
    {
    }

    private Listing(
        Guid id,
        Guid publisherUserId,
        PlatformListing.ListingSubjectReference subjectReference,
        long version)
        : base(id)
    {
        if (publisherUserId == Guid.Empty)
            throw new ArgumentException(
                "Publisher user identifier cannot be empty.",
                nameof(publisherUserId));

        ArgumentNullException.ThrowIfNull(subjectReference);

        if (version <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(version),
                version,
                "Listing version must be positive.");

        if (subjectReference.SubjectType != MarketListingSubjectTypes.Property)
            throw new ArgumentException(
                "Unsupported Market listing subject type.",
                nameof(subjectReference));

        PublisherUserId = publisherUserId;
        SubjectReference = subjectReference;
        Version = version;
        Status = ListingStatus.Draft;
    }

    public Guid PublisherUserId { get; }

    public PlatformListing.ListingSubjectReference SubjectReference { get; }

    public long Version { get; }

    public ListingStatus Status { get; private set; }

    public ListingContext? Context { get; private set; }

    public ListingHeadline? Headline { get; private set; }

    public ListingPrice? Price { get; private set; }

    public ListingAvailableFromDate? AvailableFromDate { get; private set; }

    public ListingContactOptions? Contact { get; private set; }

    public ListingClosureReason? ClosureReason { get; private set; }

    public const int MaximumModerationNoteLength = 500;

    /// <summary>Why a moderator suspended the listing; shown to the publisher.</summary>
    public string? ModerationNote { get; private set; }

    /// <summary>Publication data and photos can change in every status except <see cref="ListingStatus.Closed"/>.</summary>
    public bool IsEditable => Status != ListingStatus.Closed;

    public bool IsPubliclyVisible => Status == ListingStatus.Published;

    public static Listing Restore(
        Guid id,
        Guid publisherUserId,
        PlatformListing.ListingSubjectReference subjectReference,
        long version) =>
        new(id, publisherUserId, subjectReference, version);

    public void SetContext(ListingContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        EnsureEditable();

        Context = context;
    }

    public void SetHeadline(ListingHeadline headline)
    {
        ArgumentNullException.ThrowIfNull(headline);
        EnsureEditable();

        Headline = headline;
    }

    public void SetPrice(ListingPrice price)
    {
        ArgumentNullException.ThrowIfNull(price);
        EnsureEditable();

        Price = price;
    }

    public void SetAvailableFromDate(ListingAvailableFromDate availableFromDate)
    {
        ArgumentNullException.ThrowIfNull(availableFromDate);
        EnsureEditable();

        AvailableFromDate = availableFromDate;
    }

    public void ClearAvailableFromDate()
    {
        EnsureEditable();

        AvailableFromDate = null;
    }

    public void SetContact(ListingContactOptions contact)
    {
        ArgumentNullException.ThrowIfNull(contact);
        EnsureEditable();

        Contact = contact;
    }

    public void ClearContact()
    {
        EnsureEditable();

        Contact = null;
    }

    public void Publish()
    {
        if (Status != ListingStatus.Draft)
            throw new InvalidOperationException(
                "Only a Draft listing can be published.");

        if (Context is null || Headline is null || Price is null)
            throw new InvalidOperationException(
                "Listing is not ready for publication.");

        Status = ListingStatus.Published;
    }

    /// <summary>Hides a Published listing temporarily.</summary>
    public void Pause()
    {
        if (Status != ListingStatus.Published)
            throw new InvalidOperationException(
                "Only a Published listing can be paused.");

        Status = ListingStatus.Paused;
    }

    /// <summary>Makes a Paused listing public again.</summary>
    public void Resume()
    {
        if (Status != ListingStatus.Paused)
            throw new InvalidOperationException(
                "Only a Paused listing can be resumed.");

        Status = ListingStatus.Published;
    }

    /// <summary>Hides a Published or Paused listing by moderator decision.</summary>
    public void Suspend(string note)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(note);

        string trimmed = note.Trim();
        if (trimmed.Length > MaximumModerationNoteLength)
            throw new ArgumentException("Moderation note is too long.", nameof(note));

        if (Status is not (ListingStatus.Published or ListingStatus.Paused))
            throw new InvalidOperationException(
                "Only a Published or Paused listing can be suspended.");

        Status = ListingStatus.Suspended;
        ModerationNote = trimmed;
    }

    /// <summary>Lifts a moderator suspension and makes the listing public again.</summary>
    public void Reinstate()
    {
        if (Status != ListingStatus.Suspended)
            throw new InvalidOperationException(
                "Only a Suspended listing can be reinstated.");

        Status = ListingStatus.Published;
        ModerationNote = null;
    }

    /// <summary>Ends a Published, Paused or Suspended listing permanently.</summary>
    public void Close(ListingClosureReason reason)
    {
        if (!Enum.IsDefined(reason))
        {
            throw new ArgumentOutOfRangeException(
                nameof(reason),
                reason,
                "Unsupported closure reason.");
        }

        if (Status is not (ListingStatus.Published or ListingStatus.Paused or ListingStatus.Suspended))
            throw new InvalidOperationException(
                "Only a Published, Paused or Suspended listing can be closed.");

        Status = ListingStatus.Closed;
        ClosureReason = reason;
        ModerationNote = null;
    }

    private void EnsureEditable()
    {
        // Setters never clear required publication data, so editing a Published or Paused
        // listing cannot make it unready for publication.
        if (!IsEditable)
            throw new InvalidOperationException(
                "A Closed listing cannot be edited.");
    }
}
