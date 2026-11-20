using Diyarak.Platform.BuildingBlocks;
using Xunit;

namespace Diyarak.Platform.Identity.Application.Tests;

public sealed class SessionUseCaseTests
{
    private const string Phone = "+963912345678";

    private readonly InMemoryIdentityStore _store = new();
    private readonly CapturingOtpSender _sender = new();
    private readonly FakeClock _clock = new();
    private readonly FakeTokenIssuer _issuer = new();

    [Fact]
    public async Task Sign_in_issues_refresh_token_and_stores_only_its_hash()
    {
        PhoneSignInResult signIn = await SignInAsync();

        Assert.NotNull(signIn.RefreshToken);
        Assert.Equal(_clock.UtcNow + UserSession.IdleLifetime, signIn.RefreshToken!.ExpiresAt);
        UserSession session = Assert.Single(_store.Sessions.Values);
        Assert.Equal(signIn.UserId, session.UserId);
        Assert.DoesNotContain(session.CurrentTokenHash, signIn.RefreshToken.Value, StringComparison.Ordinal);
        Assert.StartsWith(session.Id.ToString("N") + ".", signIn.RefreshToken.Value, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Refresh_rotates_token_and_issues_new_access_token()
    {
        PhoneSignInResult signIn = await SignInAsync();
        _clock.UtcNow = _clock.UtcNow.AddDays(3);

        Result<RefreshedSession> refreshed = await Refresh(signIn.RefreshToken!.Value);

        Assert.True(refreshed.IsSuccess);
        Assert.NotEqual(signIn.RefreshToken.Value, refreshed.Value.RefreshToken.Value);
        Assert.Equal($"token-for-{signIn.UserId:N}", refreshed.Value.AccessToken.Value);
        Assert.Equal(_clock.UtcNow + UserSession.IdleLifetime, refreshed.Value.RefreshToken.ExpiresAt);

        Assert.True((await Refresh(refreshed.Value.RefreshToken.Value)).IsSuccess);
    }

    [Fact]
    public async Task Replaying_old_token_is_stale_briefly_then_revokes_the_session()
    {
        PhoneSignInResult signIn = await SignInAsync();
        string first = signIn.RefreshToken!.Value;
        string second = (await Refresh(first)).Value.RefreshToken.Value;

        Assert.Equal(SessionErrors.Stale, (await Refresh(first)).Error);

        _clock.UtcNow = _clock.UtcNow.AddMinutes(2);
        Assert.Equal(SessionErrors.Invalid, (await Refresh(first)).Error);

        // The legitimate holder of the newest token is signed out as well.
        Assert.Equal(SessionErrors.Invalid, (await Refresh(second)).Error);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-token")]
    [InlineData("00000000000000000000000000000000.secretsecretsecretsecretsecretsecret")]
    public async Task Malformed_or_unknown_tokens_are_invalid(string? token)
    {
        Assert.Equal(SessionErrors.Invalid, (await Refresh(token)).Error);
    }

    [Fact]
    public async Task Wrong_secret_for_existing_session_is_invalid_and_does_not_revoke()
    {
        PhoneSignInResult signIn = await SignInAsync();
        string forged = signIn.RefreshToken!.Value.Split('.')[0] + ".AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";

        Assert.Equal(SessionErrors.Invalid, (await Refresh(forged)).Error);
        Assert.True((await Refresh(signIn.RefreshToken.Value)).IsSuccess);
    }

    [Fact]
    public async Task Lost_concurrent_rotation_is_reported_as_stale()
    {
        PhoneSignInResult signIn = await SignInAsync();
        _store.FailNextSave = true;

        Assert.Equal(SessionErrors.Stale, (await Refresh(signIn.RefreshToken!.Value)).Error);
    }

    [Fact]
    public async Task Logout_revokes_only_that_session_and_reveals_nothing()
    {
        PhoneSignInResult phone1 = await SignInAsync();
        _clock.UtcNow = _clock.UtcNow.AddMinutes(1);
        PhoneSignInResult phone2 = await SignInAsync();
        var logout = new RevokeSessionUseCase(_store, _store, _clock);

        await logout.ExecuteAsync(phone1.RefreshToken!.Value);
        await logout.ExecuteAsync("garbage");

        Assert.Equal(SessionErrors.Invalid, (await Refresh(phone1.RefreshToken.Value)).Error);
        Assert.True((await Refresh(phone2.RefreshToken!.Value)).IsSuccess);
    }

    [Fact]
    public async Task Logout_all_revokes_every_session_of_the_user()
    {
        PhoneSignInResult phone1 = await SignInAsync();
        _clock.UtcNow = _clock.UtcNow.AddMinutes(1);
        PhoneSignInResult phone2 = await SignInAsync();

        await new RevokeAllSessionsUseCase(_store, _clock).ExecuteAsync(phone1.UserId);

        Assert.Equal(SessionErrors.Invalid, (await Refresh(phone1.RefreshToken!.Value)).Error);
        Assert.Equal(SessionErrors.Invalid, (await Refresh(phone2.RefreshToken!.Value)).Error);
    }

    private Task<Result<RefreshedSession>> Refresh(string? token) =>
        new RefreshSessionUseCase(_store, _store, _issuer, _clock).ExecuteAsync(token);

    private async Task<PhoneSignInResult> SignInAsync()
    {
        Assert.True((await new RequestPhoneVerificationUseCase(_store, _store, _sender, _clock).ExecuteAsync(Phone)).IsSuccess);
        Result<PhoneSignInResult> result =
            await new VerifyPhoneUseCase(_store, _store, _store, _issuer, _clock, _store).ExecuteAsync(Phone, _sender.Sent[^1].Code);
        Assert.True(result.IsSuccess);
        return result.Value;
    }
}
