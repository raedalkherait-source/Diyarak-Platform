using Microsoft.EntityFrameworkCore;

namespace Diyarak.Platform.Persistence.PostgreSql;

public sealed class PlatformDbContext(
    DbContextOptions<PlatformDbContext> options)
    : DbContext(options)
{
}
