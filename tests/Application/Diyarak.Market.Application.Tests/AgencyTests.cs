using Diyarak.Market.Application.Agencies;
using Diyarak.Market.Company;
using Diyarak.Platform.BuildingBlocks;
using Xunit;

namespace Diyarak.Market.Application.Tests;

public sealed class AgencyTests
{
    private static readonly Guid Owner = Guid.NewGuid();

    [Fact]
    public void Verification_lifecycle_and_rename_resets_the_badge()
    {
        Agency agency = Agency.Create(Guid.NewGuid(), Owner, "  مكتب الشام العقاري ", "وصف", "Damascus", "دمشق");
        Assert.Equal("مكتب الشام العقاري", agency.Name);
        Assert.Equal(AgencyVerificationStatus.None, agency.VerificationStatus);

        Assert.Throws<InvalidOperationException>(() => agency.Verify(DateTimeOffset.UnixEpoch));
        agency.RequestVerification("SY-12345");
        Assert.Throws<InvalidOperationException>(() => agency.RequestVerification("again"));
        agency.Verify(DateTimeOffset.UnixEpoch);
        Assert.True(agency.IsVerified);

        agency.UpdateProfile("مكتب الشام العقاري", "وصف جديد", "Damascus", "دمشق");
        Assert.True(agency.IsVerified); // same name keeps the badge

        agency.UpdateProfile("مكتب آخر", null, "Damascus", "دمشق");
        Assert.Equal(AgencyVerificationStatus.None, agency.VerificationStatus);
        Assert.Null(agency.VerifiedAt);
    }

    [Fact]
    public void Rejection_needs_a_note_and_allows_a_new_request()
    {
        Agency agency = Agency.Create(Guid.NewGuid(), Owner, "مكتب حلب", null, null, "حلب");
        agency.RequestVerification("123");
        Assert.Throws<ArgumentException>(() => agency.Reject("  "));
        agency.Reject("الرقم غير صحيح");
        Assert.Equal(AgencyVerificationStatus.Rejected, agency.VerificationStatus);

        agency.RequestVerification("456");
        Assert.Equal(AgencyVerificationStatus.Pending, agency.VerificationStatus);
        Assert.Null(agency.VerificationNote);
    }

    [Theory]
    [InlineData("م", "دمشق")]
    [InlineData("مكتب\u0007", "دمشق")]
    [InlineData("مكتب", "")]
    public void Invalid_profiles_are_rejected(string name, string city)
    {
        Assert.Throws<ArgumentException>(() => Agency.Create(Guid.NewGuid(), Owner, name, null, null, city));
    }

    [Fact]
    public async Task Save_creates_then_updates_with_the_expected_version()
    {
        var repository = new Repository();
        var useCase = new SaveMyAgencyUseCase(repository, new StubMarketTransactionRunner());

        Result<Agency> created = await useCase.ExecuteAsync(Owner, new SaveAgencyCommand("مكتب الريف", null, "RifDimashq", "جرمانا", null));
        Assert.True(created.IsSuccess);

        Result<Agency> stale = await useCase.ExecuteAsync(Owner, new SaveAgencyCommand("مكتب الريف", null, "RifDimashq", "جرمانا", 5));
        Assert.Equal(AgencyErrors.ConcurrentModification, stale.Error);

        Result<Agency> updated = await useCase.ExecuteAsync(Owner, new SaveAgencyCommand("مكتب الريف الجديد", null, "RifDimashq", "جرمانا", created.Value.Version));
        Assert.True(updated.IsSuccess);
        Assert.Equal("مكتب الريف الجديد", repository.Items.Single().Name);
    }

    [Theory]
    [InlineData("Narnia")]
    [InlineData("3")]
    public async Task Unknown_governorates_are_rejected(string governorate)
    {
        Result<Agency> result = await new SaveMyAgencyUseCase(new Repository(), new StubMarketTransactionRunner())
            .ExecuteAsync(Owner, new SaveAgencyCommand("مكتب", null, governorate, "دمشق", null));

        Assert.Equal(AgencyErrors.InvalidRequest, result.Error);
    }

    [Fact]
    public async Task Moderators_verify_and_reject_only_pending_agencies()
    {
        var repository = new Repository();
        Agency agency = Agency.Create(Guid.NewGuid(), Owner, "مكتب حمص", null, "Homs", "حمص");
        repository.Items.Add(agency);
        var decide = new DecideAgencyVerificationUseCase(repository, new Clock());

        Assert.Equal(AgencyErrors.InvalidTransition, (await decide.ExecuteAsync(agency.Id, AgencyVerificationDecision.Verify, null)).Error);

        Result<Agency> requested = await new RequestAgencyVerificationUseCase(repository).ExecuteAsync(Owner, "HMS-1", agency.Version);
        Assert.True(requested.IsSuccess);

        Result<Agency> verified = await decide.ExecuteAsync(agency.Id, AgencyVerificationDecision.Verify, null);
        Assert.True(verified.IsSuccess);
        Assert.Equal(Clock.Now, verified.Value.VerifiedAt);
    }

    private sealed class Clock : IClock
    {
        public static readonly DateTimeOffset Now = new(2026, 11, 21, 12, 0, 0, TimeSpan.Zero);

        public DateTimeOffset UtcNow => Now;
    }

    private sealed class Repository : IAgencyRepository
    {
        public List<Agency> Items { get; } = [];

        public Task<Agency?> FindByIdAsync(Guid agencyId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.SingleOrDefault(item => item.Id == agencyId));

        public Task<Agency?> FindByOwnerAsync(Guid ownerUserId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.SingleOrDefault(item => item.OwnerUserId == ownerUserId));

        public Task<IReadOnlyDictionary<Guid, Agency>> FindByOwnersAsync(IReadOnlyCollection<Guid> ownerUserIds, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<Guid, Agency>>(Items.Where(item => ownerUserIds.Contains(item.OwnerUserId)).ToDictionary(item => item.OwnerUserId));

        public Task<IReadOnlyList<Agency>> ListByStatusAsync(AgencyVerificationStatus status, int take, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Agency>>([.. Items.Where(item => item.VerificationStatus == status)]);

        public Task<bool> TryAddAsync(Agency agency, CancellationToken cancellationToken = default)
        {
            Items.Add(agency);
            return Task.FromResult(true);
        }

        public Task<bool> TrySaveAsync(Agency agency, long expectedVersion, CancellationToken cancellationToken = default)
        {
            int index = Items.FindIndex(item => item.Id == agency.Id);
            Items[index] = Agency.Restore(
                agency.Id, agency.OwnerUserId, expectedVersion + 1, agency.Name, agency.Description, agency.Governorate, agency.City,
                agency.VerificationStatus, agency.RegistrationNumber, agency.VerificationNote, agency.VerifiedAt);
            return Task.FromResult(true);
        }
    }
}
