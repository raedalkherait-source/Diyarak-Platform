namespace Diyarak.Platform.Identity;

public enum PhoneVerificationOutcome
{
    Verified = 1,
    InvalidCode = 2,
    Expired = 3,
    TooManyAttempts = 4,
    AlreadyUsed = 5
}
