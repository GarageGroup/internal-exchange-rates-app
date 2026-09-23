namespace GarageGroup.Internal.ExchangeRates;

public readonly record struct ExchangeRateGetIn
{
    public required string BaseCurrency { get; init; }

    public required string QuoteCurrency { get; init; }
}
