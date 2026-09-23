using System.Text.Json.Serialization;

namespace GarageGroup.Internal.ExchangeRates;

internal sealed record class CurrentExchangeRateTableEntity
{
    public required string PartitionKey { get; init; }

    public required string RowKey { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Rate { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Source { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? FetchedAtUtc { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ProviderTimestampUtc { get; init; }
}
