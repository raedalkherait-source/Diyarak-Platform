using Diyarak.Platform.BuildingBlocks;

namespace Diyarak.Platform.Identity.Application;

public static class PhoneAuthenticationErrors
{
    public static Error InvalidPhoneNumber { get; } =
        Error.Validation(
            "identity.phone.invalid_phone_number",
            "The phone number must use international E.164 format, for example +963912345678.");

    public static Error InvalidCodeFormat { get; } =
        Error.Validation(
            "identity.phone.invalid_code_format",
            $"The verification code must be {PhoneVerificationCode.Length} digits.");

    public static Error TooManyRequests { get; } =
        Error.Conflict(
            "identity.phone.too_many_requests",
            "Too many verification codes were requested for this phone number. Try again later.");

    public static Error VerificationFailed { get; } =
        Error.Validation(
            "identity.phone.verification_failed",
            "The verification code is invalid or has expired. Request a new code.");

    public static Error ConcurrentModification { get; } =
        Error.Conflict(
            "identity.phone.concurrent_modification",
            "The verification could not be completed because of a concurrent request. Try again.");

    public static Error DeliveryFailed { get; } =
        Error.Failure(
            "identity.phone.delivery_failed",
            "The verification code could not be sent right now. Try again in a few minutes.");
}
