using Diyarak.Market.Application.Photos;
using Diyarak.Market.Listing;
using Diyarak.Platform.Persistence.PostgreSql.Market;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Diyarak.Platform.Persistence.PostgreSql.Tests;

public sealed class PostgreSqlListingPhotoRepositoryTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 20, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Photos_round_trip_in_position_order_and_group_by_listing()
    {
        string database = Guid.NewGuid().ToString();
        Guid first = Guid.NewGuid();
        Guid second = Guid.NewGuid();

        await using (PlatformDbContext context = CreateContext(database))
        {
            var repository = new PostgreSqlListingPhotoRepository(context);
            Assert.True(await repository.TryAddAsync(CreatePhoto(first, 1, ListingPhotoFormat.Png)));
            Assert.True(await repository.TryAddAsync(CreatePhoto(first, 0, ListingPhotoFormat.Jpeg)));
            Assert.True(await repository.TryAddAsync(CreatePhoto(second, 0, ListingPhotoFormat.Webp)));
        }

        await using (PlatformDbContext context = CreateContext(database))
        {
            var repository = new PostgreSqlListingPhotoRepository(context);

            IReadOnlyList<ListingPhoto> photos = await repository.ListByListingAsync(first);
            Assert.Equal([0, 1], photos.Select(photo => photo.Position));
            Assert.Equal(ListingPhotoFormat.Jpeg, photos[0].Format);
            Assert.Equal(Now, photos[0].UploadedAt);

            IReadOnlyDictionary<Guid, IReadOnlyList<ListingPhoto>> grouped =
                await repository.ListByListingsAsync([first, second, Guid.NewGuid()]);
            Assert.Equal(2, grouped[first].Count);
            Assert.Single(grouped[second]);
            Assert.Equal(2, grouped.Count);

            Assert.Empty(await repository.ListByListingsAsync([]));

            ListingPhoto found = (await repository.FindAsync(first, photos[1].Id))!;
            Assert.Equal(photos[1].StorageKey, found.StorageKey);
            Assert.Null(await repository.FindAsync(second, photos[1].Id));

            await repository.DeleteAsync(found);
            Assert.Single(await repository.ListByListingAsync(first));
        }
    }

    [Fact]
    public void Model_enforces_unique_position_and_storage_key()
    {
        var options = new DbContextOptionsBuilder<PlatformDbContext>()
            .UseNpgsql("Host=localhost;Database=diyarak_model_test")
            .Options;

        using var context = new PlatformDbContext(options);
        var entity = context.GetService<Microsoft.EntityFrameworkCore.Metadata.IDesignTimeModel>()
            .Model
            .FindEntityType(typeof(MarketListingPhotoRecord))!;

        Assert.Equal("market", entity.GetSchema());
        Assert.Equal("listing_photos", entity.GetTableName());
        Assert.Contains(
            entity.GetIndexes(),
            index => index.IsUnique &&
                index.Properties.Select(property => property.Name).SequenceEqual(
                    [nameof(MarketListingPhotoRecord.ListingId), nameof(MarketListingPhotoRecord.Position)]));
        Assert.Contains(
            entity.GetIndexes(),
            index => index.IsUnique &&
                index.Properties.Single().Name == nameof(MarketListingPhotoRecord.StorageKey));
        Assert.Contains(
            entity.GetCheckConstraints(),
            constraint => constraint.Name == "ck_market_listing_photos_position_range");
    }

    [Fact]
    public void Persistence_registers_photo_repository()
    {
        var services = new ServiceCollection();
        services.AddPostgreSqlPersistence("Host=localhost;Database=diyarak_di_test");

        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();

        Assert.IsType<PostgreSqlListingPhotoRepository>(
            scope.ServiceProvider.GetRequiredService<IListingPhotoRepository>());
    }

    private static ListingPhoto CreatePhoto(Guid listingId, int position, ListingPhotoFormat format)
    {
        Guid id = Guid.NewGuid();
        return new ListingPhoto(
            id,
            listingId,
            ListingPhoto.CreateStorageKey(listingId, id, format),
            format,
            100 + position,
            position,
            Now);
    }

    private static PlatformDbContext CreateContext(string database) =>
        new(new DbContextOptionsBuilder<PlatformDbContext>().UseInMemoryDatabase(database).Options);
}
