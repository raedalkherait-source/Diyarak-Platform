using Diyarak.Market.Application.Agencies;
using Diyarak.Market.Company;
using Diyarak.Market.Listing;
using Diyarak.Platform.BuildingBlocks;
using Diyarak.Platform.Listing;
using Xunit;
using MarketListing = Diyarak.Market.Listing.Listing;

namespace Diyarak.Market.Application.Tests;

public sealed class AgencyPackageUseCaseTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 21, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Package_creation_validates_inputs()
    {
        Assert.Throws<ArgumentException>(() => AgencyPackage.Create(Guid.NewGuid(), Guid.Empty, 10, Now.AddDays(30), null, Now));
        Assert.Throws<ArgumentOutOfRangeException>(() => AgencyPackage.Create(Guid.NewGuid(), Guid.NewGuid(), 0, Now.AddDays(30), null, Now));
        Assert.Throws<ArgumentException>(() => AgencyPackage.Create(Guid.NewGuid(), Guid.NewGuid(), 10, Now.AddSeconds(-1), null, Now));
    }

    [Fact]
    public void Package_is_active_before_expiry_and_inactive_after()
    {
        AgencyPackage package = AgencyPackage.Create(Guid.NewGuid(), Guid.NewGuid(), 20, Now.AddDays(30), "test", Now);

        Assert.True(package.IsActive(Now));
        Assert.True(package.IsActive(Now.AddDays(29)));
        Assert.False(package.IsActive(Now.AddDays(31)));
    }

    [Fact]
    public void Revoking_a_package_makes_it_inactive()
    {
        AgencyPackage package = AgencyPackage.Create(Guid.NewGuid(), Guid.NewGuid(), 20, Now.AddDays(30), null, Now);
        Assert.True(package.IsActive(Now));

        package.Revoke(Now.AddDays(1));
        Assert.False(package.IsActive(Now));
        Assert.True(package.IsRevoked);
        Assert.NotNull(package.RevokedAt);
    }

    [Fact]
    public void Revoking_twice_throws()
    {
        AgencyPackage package = AgencyPackage.Create(Guid.NewGuid(), Guid.NewGuid(), 20, Now.AddDays(30), null, Now);
        package.Revoke(Now);
        Assert.Throws<InvalidOperationException>(() => package.Revoke(Now));
    }

    [Fact]
    public void Note_length_is_validated()
    {
        string longNote = new('x', AgencyPackage.MaximumNoteLength + 1);
        Assert.Throws<ArgumentException>(() => AgencyPackage.Create(Guid.NewGuid(), Guid.NewGuid(), 10, Now.AddDays(30), longNote, Now));
    }

    [Fact]
    public async Task Assign_package_to_existing_agency()
    {
        Agency agency = Agency.Create(Guid.NewGuid(), Guid.NewGuid(), "Test Agency", null, null, "دمشق");
        var agencies = new InMemoryAgencyRepository();
        await agencies.TryAddAsync(agency);
        var packages = new InMemoryPackageRepository();
        var clock = new FakeClock(Now);

        var useCase = new AssignAgencyPackageUseCase(agencies, packages, clock);
        Result<AgencyPackage> result = await useCase.ExecuteAsync(agency.Id, 50, 90, "Premium package");

        Assert.True(result.IsSuccess);
        Assert.Equal(50, result.Value.ListingQuota);
        Assert.Equal(Now.AddDays(90), result.Value.ExpiresAt);
        Assert.Equal("Premium package", result.Value.Note);
    }

    [Fact]
    public async Task Assign_rejects_nonexistent_agency()
    {
        var agencies = new InMemoryAgencyRepository();
        var packages = new InMemoryPackageRepository();
        var clock = new FakeClock(Now);

        var useCase = new AssignAgencyPackageUseCase(agencies, packages, clock);
        Result<AgencyPackage> result = await useCase.ExecuteAsync(Guid.NewGuid(), 50, 90, null);

        Assert.False(result.IsSuccess);
        Assert.Equal(AgencyPackageErrors.AgencyNotFound, result.Error);
    }

    [Fact]
    public async Task Assign_rejects_bad_inputs()
    {
        var agencies = new InMemoryAgencyRepository();
        var packages = new InMemoryPackageRepository();
        var clock = new FakeClock(Now);

        var useCase = new AssignAgencyPackageUseCase(agencies, packages, clock);
        Assert.Equal(AgencyPackageErrors.InvalidRequest, (await useCase.ExecuteAsync(Guid.Empty, 50, 90, null)).Error);
        Assert.Equal(AgencyPackageErrors.InvalidRequest, (await useCase.ExecuteAsync(Guid.NewGuid(), 0, 90, null)).Error);
        Assert.Equal(AgencyPackageErrors.InvalidRequest, (await useCase.ExecuteAsync(Guid.NewGuid(), 50, 0, null)).Error);
        Assert.Equal(AgencyPackageErrors.InvalidRequest, (await useCase.ExecuteAsync(Guid.NewGuid(), 50, 366, null)).Error);
    }

    [Fact]
    public async Task Revoke_package()
    {
        Agency agency = Agency.Create(Guid.NewGuid(), Guid.NewGuid(), "Test", null, null, "حلب");
        var agencies = new InMemoryAgencyRepository();
        await agencies.TryAddAsync(agency);
        var packages = new InMemoryPackageRepository();
        var clock = new FakeClock(Now);

        var assignUseCase = new AssignAgencyPackageUseCase(agencies, packages, clock);
        Result<AgencyPackage> assigned = await assignUseCase.ExecuteAsync(agency.Id, 50, 90, null);

        var revokeUseCase = new RevokeAgencyPackageUseCase(packages, clock);
        Result result = await revokeUseCase.ExecuteAsync(assigned.Value.Id);
        Assert.True(result.IsSuccess);

        AgencyPackage? revoked = await packages.FindByIdAsync(assigned.Value.Id);
        Assert.NotNull(revoked);
        Assert.True(revoked.IsRevoked);
    }

    [Fact]
    public async Task Revoke_nonexistent_fails()
    {
        var packages = new InMemoryPackageRepository();
        var clock = new FakeClock(Now);

        var useCase = new RevokeAgencyPackageUseCase(packages, clock);
        Assert.Equal(AgencyPackageErrors.PackageNotFound, (await useCase.ExecuteAsync(Guid.NewGuid())).Error);
    }

    [Fact]
    public async Task Quota_uses_free_default_without_agency()
    {
        var agencies = new InMemoryAgencyRepository();
        var packages = new InMemoryPackageRepository();
        var counter = new FakeCounter(3);
        var clock = new FakeClock(Now);

        var useCase = new GetListingQuotaUseCase(agencies, packages, counter, clock);
        ListingQuotaInfo info = await useCase.ExecuteAsync(Guid.NewGuid());

        Assert.Equal(AgencyPackage.DefaultFreeQuota, info.Quota);
        Assert.Equal(3, info.ActiveListings);
        Assert.True(info.CanPublish);
        Assert.Null(info.ActivePackage);
    }

    [Fact]
    public async Task Quota_uses_active_package_when_present()
    {
        Agency agency = Agency.Create(Guid.NewGuid(), Guid.NewGuid(), "Test", null, null, "دمشق");
        var agencies = new InMemoryAgencyRepository();
        await agencies.TryAddAsync(agency);
        var packages = new InMemoryPackageRepository();
        AgencyPackage package = AgencyPackage.Create(Guid.NewGuid(), agency.Id, 100, Now.AddDays(30), null, Now);
        await packages.AddAsync(package);
        var counter = new FakeCounter(50);
        var clock = new FakeClock(Now);

        var useCase = new GetListingQuotaUseCase(agencies, packages, counter, clock);
        ListingQuotaInfo info = await useCase.ExecuteAsync(agency.OwnerUserId);

        Assert.Equal(100, info.Quota);
        Assert.Equal(50, info.ActiveListings);
        Assert.Equal(50, info.Remaining);
        Assert.True(info.CanPublish);
        Assert.NotNull(info.ActivePackage);
    }

    [Fact]
    public async Task Publish_blocked_when_quota_exceeded()
    {
        Guid userId = Guid.NewGuid();
        var listings = new InMemoryListingRepository();
        var propertyChecker = new FakePropertyChecker(true);
        var agencies = new InMemoryAgencyRepository();
        var packages = new InMemoryPackageRepository();
        var counter = new FakeCounter(AgencyPackage.DefaultFreeQuota);
        var clock = new FakeClock(Now);
        var txRunner = new FakeTransactionRunner();

        MarketListing listing = Create(userId, publish: false);
        await listings.AddAsync(listing);

        var useCase = new PublishListingUseCase(
            listings, propertyChecker, txRunner, counter, agencies, packages, clock);

        Result result = await useCase.ExecuteAsync(listing.Id, userId);
        Assert.False(result.IsSuccess);
        Assert.Equal(PublishListingErrors.QuotaExceeded, result.Error);
    }

    [Fact]
    public async Task Publish_allowed_when_under_quota()
    {
        Guid userId = Guid.NewGuid();
        var listings = new InMemoryListingRepository();
        var propertyChecker = new FakePropertyChecker(true);
        var agencies = new InMemoryAgencyRepository();
        var packages = new InMemoryPackageRepository();
        var counter = new FakeCounter(AgencyPackage.DefaultFreeQuota - 1);
        var clock = new FakeClock(Now);
        var txRunner = new FakeTransactionRunner();

        MarketListing listing = Create(userId, publish: false);
        await listings.AddAsync(listing);

        var useCase = new PublishListingUseCase(
            listings, propertyChecker, txRunner, counter, agencies, packages, clock);

        Result result = await useCase.ExecuteAsync(listing.Id, userId);
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Publish_allowed_with_active_package_raising_quota()
    {
        Guid userId = Guid.NewGuid();
        Agency agency = Agency.Create(Guid.NewGuid(), userId, "My Agency", null, null, "دمشق");
        var agencies = new InMemoryAgencyRepository();
        await agencies.TryAddAsync(agency);
        var packages = new InMemoryPackageRepository();
        AgencyPackage package = AgencyPackage.Create(Guid.NewGuid(), agency.Id, 100, Now.AddDays(30), null, Now);
        await packages.AddAsync(package);
        var counter = new FakeCounter(50);
        var clock = new FakeClock(Now);
        var txRunner = new FakeTransactionRunner();
        var listings = new InMemoryListingRepository();
        var propertyChecker = new FakePropertyChecker(true);

        MarketListing listing = Create(userId, publish: false);
        await listings.AddAsync(listing);

        var useCase = new PublishListingUseCase(
            listings, propertyChecker, txRunner, counter, agencies, packages, clock);

        Result result = await useCase.ExecuteAsync(listing.Id, userId);
        Assert.True(result.IsSuccess);
    }

    private static MarketListing Create(Guid userId, bool publish)
    {
        var subjectReference = new ListingSubjectReference(Guid.NewGuid(), MarketListingSubjectTypes.Property);
        var listing = new MarketListing(Guid.NewGuid(), userId, subjectReference);
        listing.SetContext(new ListingContext(PublishingRole.Owner, TransactionIntent.Sell));
        listing.SetHeadline(new ListingHeadline("Test listing"));
        listing.SetPrice(ListingPrice.OnRequest());
        listing.SetContact(new ListingContactOptions(true, true));

        if (publish)
            listing.Publish(Now);

        return listing;
    }

    private sealed class InMemoryAgencyRepository : IAgencyRepository
    {
        private readonly List<Agency> _agencies = [];

        public Task<Agency?> FindByIdAsync(Guid agencyId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_agencies.Find(a => a.Id == agencyId));

        public Task<Agency?> FindByOwnerAsync(Guid ownerUserId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_agencies.Find(a => a.OwnerUserId == ownerUserId));

        public Task<IReadOnlyDictionary<Guid, Agency>> FindByOwnersAsync(IReadOnlyCollection<Guid> ownerUserIds, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<Guid, Agency>>(
                _agencies.Where(a => ownerUserIds.Contains(a.OwnerUserId)).ToDictionary(a => a.OwnerUserId));

        public Task<IReadOnlyDictionary<Guid, Agency>> FindByIdsAsync(IReadOnlyCollection<Guid> agencyIds, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<Guid, Agency>>(
                _agencies.Where(a => agencyIds.Contains(a.Id)).ToDictionary(a => a.Id));

        public Task<IReadOnlyList<Agency>> ListByStatusAsync(AgencyVerificationStatus status, int take, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Agency>>([.. _agencies.Where(a => a.VerificationStatus == status).Take(take)]);

        public Task<bool> TryAddAsync(Agency agency, CancellationToken cancellationToken = default)
        {
            if (_agencies.Exists(a => a.OwnerUserId == agency.OwnerUserId))
                return Task.FromResult(false);
            _agencies.Add(agency);
            return Task.FromResult(true);
        }

        public Task<bool> TrySaveAsync(Agency agency, long expectedVersion, CancellationToken cancellationToken = default) =>
            Task.FromResult(true);
    }

    private sealed class InMemoryPackageRepository : IAgencyPackageRepository
    {
        private readonly List<AgencyPackage> _packages = [];

        public Task<AgencyPackage?> FindByIdAsync(Guid packageId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_packages.Find(p => p.Id == packageId));

        public Task<AgencyPackage?> FindActiveByAgencyAsync(Guid agencyId, DateTimeOffset now, CancellationToken cancellationToken = default) =>
            Task.FromResult(_packages.Find(p => p.AgencyId == agencyId && p.IsActive(now)));

        public Task<IReadOnlyList<AgencyPackage>> ListByAgencyAsync(Guid agencyId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<AgencyPackage>>([.. _packages.Where(p => p.AgencyId == agencyId)]);

        public Task AddAsync(AgencyPackage package, CancellationToken cancellationToken = default)
        {
            _packages.Add(package);
            return Task.CompletedTask;
        }

        public Task<bool> TrySaveAsync(AgencyPackage package, CancellationToken cancellationToken = default)
        {
            int index = _packages.FindIndex(p => p.Id == package.Id);
            if (index >= 0) _packages[index] = package;
            return Task.FromResult(index >= 0);
        }
    }

    private sealed class InMemoryListingRepository : IMarketListingRepository
    {
        private readonly List<MarketListing> _listings = [];

        public Task<MarketListing?> FindByIdAsync(Guid listingId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_listings.Find(l => l.Id == listingId));

        public Task<IReadOnlyList<MarketListing>> FindByPublisherUserIdAsync(Guid publisherUserId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<MarketListing>>([.. _listings.Where(l => l.PublisherUserId == publisherUserId)]);

        public Task AddAsync(MarketListing listing, CancellationToken cancellationToken = default)
        {
            _listings.Add(listing);
            return Task.CompletedTask;
        }

        public Task<bool> TrySaveAsync(MarketListing listing, long expectedVersion, CancellationToken cancellationToken = default) =>
            Task.FromResult(true);
    }

    private sealed class FakePropertyChecker(bool exists) : IPropertyExistenceChecker
    {
        public Task<bool> ExistsAsync(Guid propertyId, CancellationToken cancellationToken = default) =>
            Task.FromResult(exists);
    }

    private sealed class FakeCounter(int count) : IActiveListingCounter
    {
        public Task<int> CountActiveByPublisherAsync(Guid publisherUserId, CancellationToken cancellationToken = default) =>
            Task.FromResult(count);
    }

    private sealed class FakeClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset UtcNow => now;
    }

    private sealed class FakeTransactionRunner : IMarketTransactionRunner
    {
        public Task<Result> ExecuteAsync(Func<CancellationToken, Task<Result>> operation, CancellationToken cancellationToken = default) =>
            operation(cancellationToken);

        public Task<Result<T>> ExecuteAsync<T>(Func<CancellationToken, Task<Result<T>>> operation, CancellationToken cancellationToken = default) =>
            operation(cancellationToken);
    }
}
