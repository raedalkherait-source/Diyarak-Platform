using Diyarak.Platform.BuildingBlocks;
using Diyarak.Platform.Domain.Primitives;

namespace Diyarak.Platform.Identity.Application;

public sealed record PhoneSignInResult(
    Guid UserId,
    bool IsNewUser,
    IssuedAccessToken AccessToken);

public sealed class VerifyPhoneUseCase
{
    private readonly IPhoneVerificationChallengeRepository _challenges;
    private readonly IPhoneAccountRepository _accounts;
    private readonly IIdentityUnitOfWork _unitOfWork;
    private readonly IAccessTokenIssuer _tokenIssuer;
    private readonly IClock _clock;

    public VerifyPhoneUseCase(
        IPhoneVerificationChallengeRepository challenges,
        IPhoneAccountRepository accounts,
        IIdentityUnitOfWork unitOfWork,
        IAccessTokenIssuer tokenIssuer,
        IClock clock)
    {
        ArgumentNullException.ThrowIfNull(challenges);
        ArgumentNullException.ThrowIfNull(accounts);
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(tokenIssuer);
        ArgumentNullException.ThrowIfNull(clock);

        _challenges = challenges;
        _accounts = accounts;
        _unitOfWork = unitOfWork;
        _tokenIssuer = tokenIssuer;
        _clock = clock;
    }

    public async Task<Result<PhoneSignInResult>> ExecuteAsync(
        string? phoneNumber,
        string? code,
        CancellationToken cancellationToken = default)
    {
        if (!PhoneNumber.TryCreate(phoneNumber, out PhoneNumber? phone))
        {
            return Result.Failure<PhoneSignInResult>(
                PhoneAuthenticationErrors.InvalidPhoneNumber);
        }

        if (!PhoneVerificationCode.IsWellFormed(code))
        {
            return Result.Failure<PhoneSignInResult>(
                PhoneAuthenticationErrors.InvalidCodeFormat);
        }

        PhoneVerificationChallenge? challenge =
            await _challenges.GetLatestAsync(phone, cancellationToken);

        if (challenge is null)
        {
            return Result.Failure<PhoneSignInResult>(
                PhoneAuthenticationErrors.VerificationFailed);
        }

        DateTimeOffset now = _clock.UtcNow;

        PhoneVerificationOutcome outcome = challenge.Attempt(
            PhoneVerificationCode.Matches(challenge.Id, code!, challenge.CodeHash),
            now);

        await _challenges.UpdateAsync(challenge, cancellationToken);

        if (outcome != PhoneVerificationOutcome.Verified)
        {
            // Persist the failed-attempt counter before reporting failure.
            if (outcome is PhoneVerificationOutcome.InvalidCode or PhoneVerificationOutcome.TooManyAttempts)
                await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Failure<PhoneSignInResult>(
                PhoneAuthenticationErrors.VerificationFailed);
        }

        PhoneAccount? account = await _accounts.FindAsync(phone, cancellationToken);
        bool isNewUser = account is null;

        if (account is null)
        {
            account = new PhoneAccount(phone, Guid.NewGuid(), now);

            await _accounts.AddAsync(
                account,
                new ExternalIdentity(
                    _tokenIssuer.Issuer,
                    account.UserId.ToString("D")),
                cancellationToken);
        }

        if (!await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return Result.Failure<PhoneSignInResult>(
                PhoneAuthenticationErrors.ConcurrentModification);
        }

        return Result.Success(
            new PhoneSignInResult(
                account.UserId,
                isNewUser,
                _tokenIssuer.Issue(account.UserId)));
    }
}
