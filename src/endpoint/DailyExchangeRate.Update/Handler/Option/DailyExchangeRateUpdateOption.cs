using System;

namespace GarageGroup.Internal.ExchangeRates;

public sealed record class DailyExchangeRateUpdateOption
{
    public required FlatArray<DailyExchangeRateCurrencyPair> CurrencyPairs { get; init; }
}

public sealed record class DailyExchangeRateCurrencyPair
{
    public required string BaseCurrency { get; init; }

    public required string QuoteCurrency { get; init; }
}
