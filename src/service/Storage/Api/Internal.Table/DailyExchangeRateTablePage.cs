using System;
using System.Text.Json.Serialization;

namespace GarageGroup.Internal.ExchangeRates;

internal sealed record class DailyExchangeRateTablePage
{
    [JsonPropertyName("value")]
    public required FlatArray<DailyExchangeRateTableEntity> Value { get; init; }
}
