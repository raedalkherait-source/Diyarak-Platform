namespace Diyarak.Market.Listing;

public enum ListingStatus
{
    Draft = 1,
    Published = 2,

    /// <summary>Temporarily hidden by the publisher; can be resumed.</summary>
    Paused = 3,

    /// <summary>Permanently ended (sold, rented or withdrawn); read-only.</summary>
    Closed = 4,

    /// <summary>Hidden by a moderator; the publisher may edit or withdraw it but not resume it.</summary>
    Suspended = 5,
}
