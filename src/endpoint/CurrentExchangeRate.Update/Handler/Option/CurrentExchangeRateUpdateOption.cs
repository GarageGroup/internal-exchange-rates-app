using System;

namespace GarageGroup.Internal.ExchangeRates;

public sealed record class CurrentExchangeRateUpdateOption
{
    public required FlatArray<CurrentExchangeRateCurrencyPair> CurrencyPairs { get; init; }
}

public sealed record class CurrentExchangeRateCurrencyPair
{
    public required string BaseCurrency { get; init; }

    public required string QuoteCurrency { get; init; }
}
