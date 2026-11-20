using Diyarak.Platform.BuildingBlocks;
using Diyarak.Platform.Domain.Primitives;

namespace Diyarak.Platform.Identity.Application;

public sealed class RequestPhoneVerificationUseCase
{
    public static readonly TimeSpan CodeLifetime = TimeSpan.FromMinutes(5);

    public static readonly TimeSpan ThrottleWindow = TimeSpan.FromMinutes(15);

    public const int MaximumCodesPerWindow = 3;

    private readonly IPhoneVerificationChallengeRepository _challenges;
    private readonly IIdentityUnitOfWork _unitOfWork;
    private readonly IOtpSender _sender;
    private readonly IClock _clock;

    public RequestPhoneVerificationUseCase(
        IPhoneVerificationChallengeRepository challenges,
        IIdentityUnitOfWork unitOfWork,
        IOtpSender sender,
        IClock clock)
    {
        ArgumentNullException.ThrowIfNull(challenges);
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(sender);
        ArgumentNullException.ThrowIfNull(clock);

        _challenges = challenges;
        _unitOfWork = unitOfWork;
        _sender = sender;
        _clock = clock;
    }

    /// <returns>The expiry time of the issued code.</returns>
    public async Task<Result<DateTimeOffset>> ExecuteAsync(
        string? phoneNumber,
        CancellationToken cancellationToken = default)
    {
        if (!PhoneNumber.TryCreate(phoneNumber, out PhoneNumber? phone))
        {
            return Result.Failure<DateTimeOffset>(
                PhoneAuthenticationErrors.InvalidPhoneNumber);
        }

        DateTimeOffset now = _clock.UtcNow;

        int recentCount = await _challenges.CountIssuedSinceAsync(
            phone,
            now - ThrottleWindow,
            cancellationToken);

        if (recentCount >= MaximumCodesPerWindow)
        {
            return Result.Failure<DateTimeOffset>(
                PhoneAuthenticationErrors.TooManyRequests);
        }

        Guid challengeId = Guid.NewGuid();
        string code = PhoneVerificationCode.Generate();

        var challenge = PhoneVerificationChallenge.Issue(
            challengeId,
            phone,
            PhoneVerificationCode.Hash(challengeId, code),
            now,
            CodeLifetime);

        await _challenges.AddAsync(challenge, cancellationToken);

        if (!await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return Result.Failure<DateTimeOffset>(
                PhoneAuthenticationErrors.ConcurrentModification);
        }

        await _sender.SendAsync(phone, code, cancellationToken);

        return Result.Success(challenge.ExpiresAt);
    }
}
