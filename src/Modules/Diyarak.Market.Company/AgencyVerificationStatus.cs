namespace Diyarak.Market.Company;

public enum AgencyVerificationStatus
{
    /// <summary>No verification requested (or the name changed after verification).</summary>
    None = 0,
    Pending = 1,
    Verified = 2,
    Rejected = 3,
}
