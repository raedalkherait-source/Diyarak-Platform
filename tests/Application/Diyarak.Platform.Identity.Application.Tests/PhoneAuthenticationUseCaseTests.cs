using Diyarak.Platform.BuildingBlocks;
using Xunit;

namespace Diyarak.Platform.Identity.Application.Tests;

public sealed class PhoneAuthenticationUseCaseTests
{
    private const string Phone = "+963912345678";

    private readonly InMemoryIdentityStore _store = new();
    private readonly CapturingOtpSender _sender = new();
    private readonly FakeClock _clock = new();
    private readonly FakeTokenIssuer _issuer = new();

    [Fact]
    public async Task Request_sends_six_digit_code_and_stores_only_its_hash()
    {
        Result<DateTimeOffset> result = await CreateRequest().ExecuteAsync(Phone);

        Assert.True(result.IsSuccess);
        Assert.Equal(_clock.UtcNow + RequestPhoneVerificationUseCase.CodeLifetime, result.Value);

        (var phone, string code) = Assert.Single(_sender.Sent);
        Assert.Equal(Phone, phone.Value);
        Assert.Matches("^[0-9]{6}$", code);

        PhoneVerificationChallenge challenge = Assert.Single(_store.Challenges.Values);
        Assert.DoesNotContain(code, challenge.CodeHash, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("0912345678")]
    [InlineData("+963 912 345 678")]
    public async Task Request_rejects_non_e164_phone_numbers(string? phone)
    {
        Result<DateTimeOffset> result = await CreateRequest().ExecuteAsync(phone);

        Assert.Equal(PhoneAuthenticationErrors.InvalidPhoneNumber, result.Error);
        Assert.Empty(_sender.Sent);
    }

    [Fact]
    public async Task Request_is_throttled_per_phone_within_window()
    {
        RequestPhoneVerificationUseCase useCase = CreateRequest();

        for (int i = 0; i < RequestPhoneVerificationUseCase.MaximumCodesPerWindow; i++)
            Assert.True((await useCase.ExecuteAsync(Phone)).IsSuccess);

        Result<DateTimeOffset> throttled = await useCase.ExecuteAsync(Phone);
        Assert.Equal(PhoneAuthenticationErrors.TooManyRequests, throttled.Error);

        _clock.UtcNow += RequestPhoneVerificationUseCase.ThrottleWindow + TimeSpan.FromSeconds(1);
        Assert.True((await useCase.ExecuteAsync(Phone)).IsSuccess);
    }

    [Fact]
    public async Task Request_does_not_send_when_challenge_cannot_be_saved()
    {
        _store.FailNextSave = true;

        Result<DateTimeOffset> result = await CreateRequest().ExecuteAsync(Phone);

        Assert.Equal(PhoneAuthenticationErrors.ConcurrentModification, result.Error);
        Assert.Empty(_sender.Sent);
    }

    [Fact]
    public async Task Verify_with_correct_code_creates_account_mapping_and_token()
    {
        string code = await RequestCodeAsync();

        Result<PhoneSignInResult> result = await CreateVerify().ExecuteAsync(Phone, code);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.IsNewUser);
        Assert.NotEqual(Guid.Empty, result.Value.UserId);
        Assert.Equal($"token-for-{result.Value.UserId:N}", result.Value.AccessToken.Value);

        PhoneAccount account = Assert.Single(_store.Accounts.Values);
        Assert.Equal(result.Value.UserId, account.UserId);

        (ExternalIdentity identity, Guid mappedUserId) = Assert.Single(_store.Mappings);
        Assert.Equal(_issuer.Issuer, identity.Issuer);
        Assert.Equal(result.Value.UserId.ToString("D"), identity.Subject);
        Assert.Equal(result.Value.UserId, mappedUserId);
    }

    [Fact]
    public async Task Verify_for_existing_account_returns_same_user()
    {
        Result<PhoneSignInResult> first =
            await CreateVerify().ExecuteAsync(Phone, await RequestCodeAsync());

        _clock.UtcNow = _clock.UtcNow.AddMinutes(1);

        Result<PhoneSignInResult> second =
            await CreateVerify().ExecuteAsync(Phone, await RequestCodeAsync());

        Assert.True(second.IsSuccess);
        Assert.False(second.Value.IsNewUser);
        Assert.Equal(first.Value.UserId, second.Value.UserId);
        Assert.Single(_store.Accounts);
        Assert.Single(_store.Mappings);
    }

    [Fact]
    public async Task Verify_code_cannot_be_used_twice()
    {
        string code = await RequestCodeAsync();
        VerifyPhoneUseCase useCase = CreateVerify();

        Assert.True((await useCase.ExecuteAsync(Phone, code)).IsSuccess);

        Result<PhoneSignInResult> replay = await useCase.ExecuteAsync(Phone, code);
        Assert.Equal(PhoneAuthenticationErrors.VerificationFailed, replay.Error);
    }

    [Fact]
    public async Task Verify_wrong_code_persists_failed_attempt_and_locks_after_maximum()
    {
        string code = await RequestCodeAsync();
        string wrong = code == "000000" ? "111111" : "000000";
        VerifyPhoneUseCase useCase = CreateVerify();

        for (int i = 0; i < PhoneVerificationChallenge.MaximumFailedAttempts; i++)
        {
            Result<PhoneSignInResult> failure = await useCase.ExecuteAsync(Phone, wrong);
            Assert.Equal(PhoneAuthenticationErrors.VerificationFailed, failure.Error);
        }

        Assert.Equal(
            PhoneVerificationChallenge.MaximumFailedAttempts,
            Assert.Single(_store.Challenges.Values).FailedAttempts);

        Result<PhoneSignInResult> locked = await useCase.ExecuteAsync(Phone, code);
        Assert.Equal(PhoneAuthenticationErrors.VerificationFailed, locked.Error);
        Assert.Empty(_store.Accounts);
    }

    [Fact]
    public async Task Verify_expired_code_fails()
    {
        string code = await RequestCodeAsync();
        _clock.UtcNow += RequestPhoneVerificationUseCase.CodeLifetime;

        Result<PhoneSignInResult> result = await CreateVerify().ExecuteAsync(Phone, code);

        Assert.Equal(PhoneAuthenticationErrors.VerificationFailed, result.Error);
    }

    [Fact]
    public async Task Verify_only_accepts_latest_code()
    {
        string firstCode = await RequestCodeAsync();
        _clock.UtcNow = _clock.UtcNow.AddSeconds(30);
        string secondCode = await RequestCodeAsync();

        if (firstCode == secondCode)
            return;

        Result<PhoneSignInResult> stale = await CreateVerify().ExecuteAsync(Phone, firstCode);
        Assert.Equal(PhoneAuthenticationErrors.VerificationFailed, stale.Error);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("12345")]
    [InlineData("1234567")]
    [InlineData("12a456")]
    public async Task Verify_rejects_malformed_codes_without_touching_challenge(string? code)
    {
        await RequestCodeAsync();

        Result<PhoneSignInResult> result = await CreateVerify().ExecuteAsync(Phone, code);

        Assert.Equal(PhoneAuthenticationErrors.InvalidCodeFormat, result.Error);
        Assert.Equal(0, Assert.Single(_store.Challenges.Values).FailedAttempts);
    }

    [Fact]
    public async Task Verify_without_requested_code_fails()
    {
        Result<PhoneSignInResult> result = await CreateVerify().ExecuteAsync(Phone, "123456");

        Assert.Equal(PhoneAuthenticationErrors.VerificationFailed, result.Error);
    }

    [Fact]
    public async Task Verify_reports_conflict_when_commit_fails()
    {
        string code = await RequestCodeAsync();
        _store.FailNextSave = true;

        Result<PhoneSignInResult> result = await CreateVerify().ExecuteAsync(Phone, code);

        Assert.Equal(PhoneAuthenticationErrors.ConcurrentModification, result.Error);
        Assert.Empty(_store.Accounts);
    }

    private async Task<string> RequestCodeAsync()
    {
        Assert.True((await CreateRequest().ExecuteAsync(Phone)).IsSuccess);
        return _sender.Sent[^1].Code;
    }

    private RequestPhoneVerificationUseCase CreateRequest() =>
        new(_store, _store, _sender, _clock);

    private VerifyPhoneUseCase CreateVerify() =>
        new(_store, _store, _store, _issuer, _clock);
}
