namespace Diyarak.Platform.BuildingBlocks;

public interface IClock
{
    public DateTimeOffset UtcNow { get; }
}
