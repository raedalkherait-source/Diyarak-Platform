namespace Diyarak.Market.Application;

public sealed record UpdatePropertyCommand(
    long ExpectedVersion,
    CreatePropertyCommand Replacement);
