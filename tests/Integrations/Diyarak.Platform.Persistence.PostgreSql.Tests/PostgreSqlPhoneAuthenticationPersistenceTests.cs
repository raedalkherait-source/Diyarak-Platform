using Diyarak.Platform.Domain.Primitives;
using Diyarak.Platform.Identity;
using Diyarak.Platform.Identity.Application;
using Diyarak.Platform.Persistence.PostgreSql.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Diyarak.Platform.Persistence.PostgreSql.Tests;

public sealed class PostgreSqlPhoneAuthenticationPersistenceTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 11, 20, 10, 0, 0, TimeSpan.Zero);

    private static readonly PhoneNumber Phone =
        PhoneNumber.Create("+963912345678");

    [Fact]
    public async Task Challenges_round_trip_and_latest_is_returned()
    {
        string database = Guid.NewGuid().ToString();

        await using (PlatformDbContext context = CreateContext(database))
        {
            var repository = new PostgreSqlPhoneVerificationChallengeRepository(context);
            await repository.AddAsync(CreateChallenge(Now));
            await repository.AddAsync(CreateChallenge(Now.AddMinutes(1)));
            await repository.AddAsync(
                PhoneVerificationChallenge.Issue(
                    Guid.NewGuid(),
                    PhoneNumber.Create("+963933333333"),
                    "other",
                    Now.AddMinutes(2),
                    TimeSpan.FromMinutes(5)));
            Assert.True(await new PostgreSqlIdentityUnitOfWork(context).SaveChangesAsync());
        }

        await using (PlatformDbContext context = CreateContext(database))
        {
            var repository = new PostgreSqlPhoneVerificationChallengeRepository(context);

            PhoneVerificationChallenge? latest = await repository.GetLatestAsync(Phone);

            Assert.NotNull(latest);
            Assert.Equal(Now.AddMinutes(1), latest!.IssuedAt);
            Assert.Equal(Now.AddMinutes(6), latest.ExpiresAt);
            Assert.Equal(Phone, latest.PhoneNumber);
            Assert.Equal(2, await repository.CountIssuedSinceAsync(Phone, Now));
            Assert.Equal(1, await repository.CountIssuedSinceAsync(Phone, Now.AddSeconds(1)));
        }
    }

    [Fact]
    public async Task Update_persists_attempts_and_consumption()
    {
        string database = Guid.NewGuid().ToString();
        PhoneVerificationChallenge challenge = CreateChallenge(Now);

        await using (PlatformDbContext context = CreateContext(database))
        {
            await new PostgreSqlPhoneVerificationChallengeRepository(context).AddAsync(challenge);
            await context.SaveChangesAsync();
        }

        await using (PlatformDbContext context = CreateContext(database))
        {
            var repository = new PostgreSqlPhoneVerificationChallengeRepository(context);
            PhoneVerificationChallenge loaded = (await repository.GetLatestAsync(Phone))!;

            loaded.Attempt(codeMatches: false, Now);
            loaded.Attempt(codeMatches: true, Now.AddMinutes(1));
            await repository.UpdateAsync(loaded);

            Assert.True(await new PostgreSqlIdentityUnitOfWork(context).SaveChangesAsync());
        }

        await using (PlatformDbContext context = CreateContext(database))
        {
            PhoneVerificationChallenge reloaded =
                (await new PostgreSqlPhoneVerificationChallengeRepository(context).GetLatestAsync(Phone))!;

            Assert.Equal(1, reloaded.FailedAttempts);
            Assert.Equal(Now.AddMinutes(1), reloaded.ConsumedAt);
        }
    }

    [Fact]
    public async Task Concurrent_consumption_of_same_challenge_is_rejected()
    {
        string database = Guid.NewGuid().ToString();

        await using (PlatformDbContext context = CreateContext(database))
        {
            await new PostgreSqlPhoneVerificationChallengeRepository(context).AddAsync(CreateChallenge(Now));
            await context.SaveChangesAsync();
        }

        await using PlatformDbContext first = CreateContext(database);
        await using PlatformDbContext second = CreateContext(database);

        var firstRepository = new PostgreSqlPhoneVerificationChallengeRepository(first);
        var secondRepository = new PostgreSqlPhoneVerificationChallengeRepository(second);

        PhoneVerificationChallenge firstCopy = (await firstRepository.GetLatestAsync(Phone))!;
        PhoneVerificationChallenge secondCopy = (await secondRepository.GetLatestAsync(Phone))!;

        Assert.Equal(PhoneVerificationOutcome.Verified, firstCopy.Attempt(true, Now));
        Assert.Equal(PhoneVerificationOutcome.Verified, secondCopy.Attempt(true, Now));

        await firstRepository.UpdateAsync(firstCopy);
        Assert.True(await new PostgreSqlIdentityUnitOfWork(first).SaveChangesAsync());

        // The second context loaded the challenge before it was consumed, so its update must
        // fail the concurrency check instead of signing in a second time.
        await secondRepository.UpdateAsync(secondCopy);
        Assert.False(await new PostgreSqlIdentityUnitOfWork(second).SaveChangesAsync());
    }

    [Fact]
    public async Task Account_creation_adds_mapping_resolved_by_existing_resolver()
    {
        string database = Guid.NewGuid().ToString();
        Guid userId = Guid.NewGuid();
        var tokenIdentity = new ExternalIdentity("https://auth.diyarak.test", userId.ToString("D"));

        await using (PlatformDbContext context = CreateContext(database))
        {
            await new PostgreSqlPhoneAccountRepository(context).AddAsync(
                new PhoneAccount(Phone, userId, Now),
                tokenIdentity);
            Assert.True(await new PostgreSqlIdentityUnitOfWork(context).SaveChangesAsync());
        }

        await using (PlatformDbContext context = CreateContext(database))
        {
            PhoneAccount? account = await new PostgreSqlPhoneAccountRepository(context).FindAsync(Phone);
            Assert.NotNull(account);
            Assert.Equal(userId, account!.UserId);
            Assert.Equal(Now, account.CreatedAt);

            Assert.Null(
                await new PostgreSqlPhoneAccountRepository(context).FindAsync(
                    PhoneNumber.Create("+963900000000")));

            Assert.Equal(
                userId,
                await new PostgreSqlExternalIdentityResolver(context).ResolveUserIdAsync(tokenIdentity));
        }
    }

    [Fact]
    public void Model_maps_phone_tables_to_identity_schema_with_constraints()
    {
        var options =
            new DbContextOptionsBuilder<PlatformDbContext>()
                .UseNpgsql("Host=localhost;Database=diyarak_model_test")
                .Options;

        using var context = new PlatformDbContext(options);

        var challenge = context.Model.FindEntityType(typeof(PhoneVerificationChallengeRecord))!;
        Assert.Equal("identity", challenge.GetSchema());
        Assert.Equal("phone_verification_challenges", challenge.GetTableName());
        Assert.True(challenge.FindProperty(nameof(PhoneVerificationChallengeRecord.FailedAttempts))!.IsConcurrencyToken);
        Assert.True(challenge.FindProperty(nameof(PhoneVerificationChallengeRecord.ConsumedAt))!.IsConcurrencyToken);
        Assert.DoesNotContain(challenge.GetProperties(), property => property.Name == "Code");

        var account = context.Model.FindEntityType(typeof(PhoneAccountRecord))!;
        Assert.Equal("identity", account.GetSchema());
        Assert.Equal("phone_accounts", account.GetTableName());
        Assert.Equal(
            [nameof(PhoneAccountRecord.PhoneNumber)],
            account.FindPrimaryKey()!.Properties.Select(property => property.Name));
        Assert.Contains(
            account.GetIndexes(),
            index => index.IsUnique && index.Properties.Single().Name == nameof(PhoneAccountRecord.UserId));
    }

    [Fact]
    public void Persistence_registers_phone_authentication_ports()
    {
        var services = new ServiceCollection();
        services.AddPostgreSqlPersistence("Host=localhost;Database=diyarak_di_test");

        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();

        Assert.IsType<PostgreSqlPhoneVerificationChallengeRepository>(
            scope.ServiceProvider.GetRequiredService<IPhoneVerificationChallengeRepository>());
        Assert.IsType<PostgreSqlPhoneAccountRepository>(
            scope.ServiceProvider.GetRequiredService<IPhoneAccountRepository>());
        Assert.IsType<PostgreSqlIdentityUnitOfWork>(
            scope.ServiceProvider.GetRequiredService<IIdentityUnitOfWork>());
    }

    private static PhoneVerificationChallenge CreateChallenge(DateTimeOffset issuedAt) =>
        PhoneVerificationChallenge.Issue(
            Guid.NewGuid(),
            Phone,
            "hash",
            issuedAt,
            TimeSpan.FromMinutes(5));

    private static PlatformDbContext CreateContext(string database) =>
        new(
            new DbContextOptionsBuilder<PlatformDbContext>()
                .UseInMemoryDatabase(database)
                .Options);
}
