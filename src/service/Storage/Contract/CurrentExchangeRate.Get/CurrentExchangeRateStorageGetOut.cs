using System;

namespace GarageGroup.Internal.ExchangeRates;

public sealed record class CurrentExchangeRateStorageGetOut
{
    public required string BaseCurrency { get; init; }

    public required string QuoteCurrency { get; init; }

    public decimal Rate { get; init; }

    public required string Source { get; init; }

    public DateTimeOffset FetchedAtUtc { get; init; }

    public DateTimeOffset? ProviderTimestampUtc { get; init; }
}
