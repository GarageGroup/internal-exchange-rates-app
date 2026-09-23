namespace GarageGroup.Internal.ExchangeRates;

public readonly record struct CurrentExchangeRateGetIn
{
    public required string BaseCurrency { get; init; }

    public required string QuoteCurrency { get; init; }
}
