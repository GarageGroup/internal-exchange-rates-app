using System;

namespace GarageGroup.Internal.ExchangeRates;

public readonly record struct DailyExchangeRateStorageGetIn
{
    public required string BaseCurrency { get; init; }

    public required string QuoteCurrency { get; init; }

    public DateOnly Date { get; init; }
}
