namespace GarageGroup.Internal.ExchangeRates;

public sealed record class StorageOption
{
    public required string CurrentRatesTableName { get; init; }

    public required string DailyRatesTableName { get; init; }
}
