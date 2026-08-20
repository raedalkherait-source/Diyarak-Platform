namespace Diyarak.Platform.BuildingBlocks.Tests;

public sealed class ClockTests
{
    [Fact] public void System_clock_returns_utc_offset() => Assert.Equal(TimeSpan.Zero, SystemClock.Instance.UtcNow.Offset);
}
