using System;

namespace GarageGroup.Internal.ExchangeRates;

public sealed record class DailyExchangeRateGetOut
{
    public required string BaseCurrency { get; init; }

    public required string QuoteCurrency { get; init; }

    public decimal Rate { get; init; }

    public required string Source { get; init; }

    public DateOnly EffectiveDate { get; init; }

    public DateTimeOffset FetchedAtUtc { get; init; }
}
