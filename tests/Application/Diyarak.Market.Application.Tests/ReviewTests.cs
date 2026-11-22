using Diyarak.Market.Application.Agencies;
using Diyarak.Market.Application.Contractors;
using Diyarak.Market.Application.Reviews;
using Diyarak.Market.Company;
using Diyarak.Platform.BuildingBlocks;
using Xunit;

namespace Diyarak.Market.Application.Tests;

public sealed class ReviewTests
{
    private static readonly Guid AuthorUserId = Guid.NewGuid();
    private static readonly Guid ContractorOwnerUserId = Guid.NewGuid();

    private static CreateReviewCommand ValidCommand(Guid subjectId) => new(
        subjectId,
        ReviewSubjectType.Contractor,
        5,
        "عمل ممتاز");

    private static Contractor CreatePublishedContractor(Guid ownerUserId)
    {
        var contractor = Contractor.Create(
            Guid.NewGuid(), ownerUserId,
            "أحمد للمقاولات", null, "Damascus", null,
            ContractorSpecialty.GeneralConstruction, "+963912345678",
            DateTimeOffset.UtcNow);
        contractor.Publish();
        return contractor;
    }

    [Fact]
    public async Task Create_review_succeeds()
    {
        var contractorRepo = new InMemoryContractorRepo();
        var contractor = CreatePublishedContractor(ContractorOwnerUserId);
        await contractorRepo.AddAsync(contractor);

        var reviewRepo = new InMemoryReviewRepo();
        var clock = new FixedClock(DateTimeOffset.UtcNow);
        var useCase = new CreateReviewUseCase(reviewRepo, contractorRepo, new InMemoryAgencyRepo(), clock);

        Result<Review> result = await useCase.ExecuteAsync(AuthorUserId, ValidCommand(contractor.Id));

        Assert.True(result.IsSuccess);
        Assert.Equal(5, result.Value.Rating);
        Assert.Equal("عمل ممتاز", result.Value.Comment);
        Assert.Single(reviewRepo.Items);
    }

    [Fact]
    public async Task Create_fails_already_reviewed()
    {
        var contractorRepo = new InMemoryContractorRepo();
        var contractor = CreatePublishedContractor(ContractorOwnerUserId);
        await contractorRepo.AddAsync(contractor);

        var reviewRepo = new InMemoryReviewRepo();
        var clock = new FixedClock(DateTimeOffset.UtcNow);
        var useCase = new CreateReviewUseCase(reviewRepo, contractorRepo, new InMemoryAgencyRepo(), clock);

        await useCase.ExecuteAsync(AuthorUserId, ValidCommand(contractor.Id));
        Result<Review> second = await useCase.ExecuteAsync(AuthorUserId, ValidCommand(contractor.Id));

        Assert.True(second.IsFailure);
        Assert.Equal("market.review.already_reviewed", second.Error.Code);
    }

    [Fact]
    public async Task Create_fails_own_profile()
    {
        var contractorRepo = new InMemoryContractorRepo();
        var contractor = CreatePublishedContractor(ContractorOwnerUserId);
        await contractorRepo.AddAsync(contractor);

        var reviewRepo = new InMemoryReviewRepo();
        var clock = new FixedClock(DateTimeOffset.UtcNow);
        var useCase = new CreateReviewUseCase(reviewRepo, contractorRepo, new InMemoryAgencyRepo(), clock);

        Result<Review> result = await useCase.ExecuteAsync(ContractorOwnerUserId, ValidCommand(contractor.Id));

        Assert.True(result.IsFailure);
        Assert.Equal("market.review.cannot_review_own", result.Error.Code);
    }

    [Fact]
    public async Task Create_fails_subject_not_found()
    {
        var reviewRepo = new InMemoryReviewRepo();
        var clock = new FixedClock(DateTimeOffset.UtcNow);
        var useCase = new CreateReviewUseCase(reviewRepo, new InMemoryContractorRepo(), new InMemoryAgencyRepo(), clock);

        Result<Review> result = await useCase.ExecuteAsync(AuthorUserId, ValidCommand(Guid.NewGuid()));

        Assert.True(result.IsFailure);
        Assert.Equal("market.review.subject_not_found", result.Error.Code);
    }

    [Fact]
    public async Task Delete_review_succeeds()
    {
        var contractorRepo = new InMemoryContractorRepo();
        var contractor = CreatePublishedContractor(ContractorOwnerUserId);
        await contractorRepo.AddAsync(contractor);

        var reviewRepo = new InMemoryReviewRepo();
        var clock = new FixedClock(DateTimeOffset.UtcNow);
        var createUseCase = new CreateReviewUseCase(reviewRepo, contractorRepo, new InMemoryAgencyRepo(), clock);
        var deleteUseCase = new DeleteReviewUseCase(reviewRepo);

        Result<Review> created = await createUseCase.ExecuteAsync(AuthorUserId, ValidCommand(contractor.Id));
        Result deleteResult = await deleteUseCase.ExecuteAsync(AuthorUserId, created.Value.Id);

        Assert.True(deleteResult.IsSuccess);
        Assert.Empty(reviewRepo.Items);
    }

    [Fact]
    public async Task Delete_fails_not_author()
    {
        var contractorRepo = new InMemoryContractorRepo();
        var contractor = CreatePublishedContractor(ContractorOwnerUserId);
        await contractorRepo.AddAsync(contractor);

        var reviewRepo = new InMemoryReviewRepo();
        var clock = new FixedClock(DateTimeOffset.UtcNow);
        var createUseCase = new CreateReviewUseCase(reviewRepo, contractorRepo, new InMemoryAgencyRepo(), clock);
        var deleteUseCase = new DeleteReviewUseCase(reviewRepo);

        Result<Review> created = await createUseCase.ExecuteAsync(AuthorUserId, ValidCommand(contractor.Id));
        Result deleteResult = await deleteUseCase.ExecuteAsync(Guid.NewGuid(), created.Value.Id);

        Assert.True(deleteResult.IsFailure);
        Assert.Equal("market.review.not_found", deleteResult.Error.Code);
    }

    [Fact]
    public void Domain_validates_rating_range()
    {
        Assert.Throws<ArgumentException>(() =>
            Review.Create(Guid.NewGuid(), AuthorUserId, Guid.NewGuid(),
                ReviewSubjectType.Contractor, 0, null, DateTimeOffset.UtcNow));

        Assert.Throws<ArgumentException>(() =>
            Review.Create(Guid.NewGuid(), AuthorUserId, Guid.NewGuid(),
                ReviewSubjectType.Contractor, 6, null, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Domain_validates_comment_length()
    {
        Assert.Throws<ArgumentException>(() =>
            Review.Create(Guid.NewGuid(), AuthorUserId, Guid.NewGuid(),
                ReviewSubjectType.Contractor, 4, new string('أ', 1001), DateTimeOffset.UtcNow));
    }

    private sealed class FixedClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset UtcNow => now;
    }

    private sealed class InMemoryReviewRepo : IReviewRepository
    {
        public List<Review> Items { get; } = [];

        public Task<Review?> FindByIdAsync(Guid id, CancellationToken ct = default) =>
            Task.FromResult(Items.Find(r => r.Id == id));

        public Task<Review?> FindByAuthorAndSubjectAsync(Guid authorUserId, Guid subjectId, CancellationToken ct = default) =>
            Task.FromResult(Items.Find(r => r.AuthorUserId == authorUserId && r.SubjectId == subjectId));

        public Task<IReadOnlyList<Review>> ListBySubjectAsync(Guid subjectId, int offset, int take, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<Review>>([.. Items.Where(r => r.SubjectId == subjectId).Skip(offset).Take(take)]);

        public Task<(double Average, int Count)?> GetSubjectAverageAsync(Guid subjectId, CancellationToken ct = default)
        {
            var subjectReviews = Items.Where(r => r.SubjectId == subjectId).ToList();
            if (subjectReviews.Count == 0)
                return Task.FromResult<(double Average, int Count)?>(null);
            return Task.FromResult<(double Average, int Count)?>((subjectReviews.Average(r => r.Rating), subjectReviews.Count));
        }

        public Task AddAsync(Review review, CancellationToken ct = default) { Items.Add(review); return Task.CompletedTask; }

        public Task DeleteAsync(Guid id, CancellationToken ct = default) { Items.RemoveAll(r => r.Id == id); return Task.CompletedTask; }
    }

    private sealed class InMemoryContractorRepo : IContractorRepository
    {
        public List<Contractor> Items { get; } = [];

        public Task<Contractor?> FindByIdAsync(Guid id, CancellationToken ct = default) =>
            Task.FromResult(Items.Find(c => c.Id == id));
        public Task<Contractor?> FindByOwnerAsync(Guid ownerUserId, CancellationToken ct = default) =>
            Task.FromResult(Items.Find(c => c.OwnerUserId == ownerUserId));
        public Task<IReadOnlyList<Contractor>> ListPublishedAsync(int offset, int take, string? governorate, ContractorSpecialty? specialty, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<Contractor>>([]);
        public Task AddAsync(Contractor contractor, CancellationToken ct = default) { Items.Add(contractor); return Task.CompletedTask; }
        public Task<bool> TrySaveAsync(Contractor contractor, long expectedVersion, CancellationToken ct = default) =>
            Task.FromResult(true);
        public Task DeleteAsync(Guid id, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class InMemoryAgencyRepo : IAgencyRepository
    {
        public List<Agency> Items { get; } = [];

        public Task<Agency?> FindByIdAsync(Guid agencyId, CancellationToken ct = default) =>
            Task.FromResult(Items.Find(a => a.Id == agencyId));
        public Task<Agency?> FindByOwnerAsync(Guid ownerUserId, CancellationToken ct = default) =>
            Task.FromResult(Items.Find(a => a.OwnerUserId == ownerUserId));
        public Task<IReadOnlyDictionary<Guid, Agency>> FindByOwnersAsync(IReadOnlyCollection<Guid> ownerUserIds, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyDictionary<Guid, Agency>>(new Dictionary<Guid, Agency>());
        public Task<IReadOnlyDictionary<Guid, Agency>> FindByIdsAsync(IReadOnlyCollection<Guid> agencyIds, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyDictionary<Guid, Agency>>(new Dictionary<Guid, Agency>());
        public Task<IReadOnlyList<Agency>> ListByStatusAsync(AgencyVerificationStatus status, int take, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<Agency>>([]);
        public Task<bool> TryAddAsync(Agency agency, CancellationToken ct = default) { Items.Add(agency); return Task.FromResult(true); }
        public Task<bool> TrySaveAsync(Agency agency, long expectedVersion, CancellationToken ct = default) =>
            Task.FromResult(true);
    }
}
