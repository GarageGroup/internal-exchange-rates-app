using System;
using System.Collections.Generic;
using System.Globalization;
using GarageGroup.Infra;

namespace GarageGroup.Internal.ExchangeRates;

internal sealed partial class StorageApi : IStorageApi
{
    private const string StorageApiVersion = "2019-02-02";

    private const string HeaderMsVersion = "x-ms-version";

    private const string HeaderAccept = "Accept";

    private const string JsonNoMetadataMediaType = "application/json;odata=nometadata";

    private const string DailyPartitionDateFormat = "yyyyMMdd";

    private const string EffectiveDateFormat = "yyyy-MM-dd";

    private static readonly FlatArray<KeyValuePair<string, string>> GetHeaders
        =
        [
            new(HeaderMsVersion, StorageApiVersion),
            new(HeaderAccept, JsonNoMetadataMediaType)
        ];

    private static readonly FlatArray<KeyValuePair<string, string>> SetHeaders
        =
        [
            new(HeaderMsVersion, StorageApiVersion),
            new(HeaderAccept, JsonNoMetadataMediaType)
        ];

    private readonly IHttpApi httpApi;

    private readonly StorageOption option;

    internal StorageApi(IHttpApi httpApi, StorageOption option)
    {
        this.httpApi = httpApi;
        this.option = option;
    }
    
    private static bool IsInvalidCurrencyPair(string? baseCurrency, string? quoteCurrency)
        =>
        IsInvalidCurrency(baseCurrency)
        || IsInvalidCurrency(quoteCurrency)
        || string.Equals(baseCurrency, quoteCurrency, StringComparison.OrdinalIgnoreCase);

    private static bool IsInvalidCurrency(string? currency)
        =>
        currency?.Length is not 3
        || char.IsAsciiLetter(currency[0]) is false
        || char.IsAsciiLetter(currency[1]) is false
        || char.IsAsciiLetter(currency[2]) is false;

    private static string NormalizeCurrency(string currency)
        => 
        currency.ToUpperInvariant();

    private static string BuildDailyPartitionKey(string baseCurrency, DateOnly date)
        => 
        $"{NormalizeCurrency(baseCurrency)}|{date.ToString(DailyPartitionDateFormat, CultureInfo.InvariantCulture)}";

    private static string FormatRate(decimal rate)
        => 
        rate.ToString("G29", CultureInfo.InvariantCulture);

    private static decimal ParseRate(string? rate)
        => 
        decimal.Parse(rate.OrEmpty(), NumberStyles.Number, CultureInfo.InvariantCulture);

    private static DateOnly ParseEffectiveDate(string? date)
        => 
        DateOnly.ParseExact(date.OrEmpty(), EffectiveDateFormat, CultureInfo.InvariantCulture);

    private static string ParseDailyBaseCurrency(string partitionKey)
    {
        var separatorIndex = partitionKey.IndexOf('|', StringComparison.Ordinal);
        return separatorIndex > 0 ? partitionKey[..separatorIndex] : partitionKey;
    }

    private static string FormatDateTimeOffset(DateTimeOffset value)
        => 
        value.ToString("O", CultureInfo.InvariantCulture);

    private static DateTimeOffset ParseDateTimeOffset(string? value)
        => 
        DateTimeOffset.Parse(value.OrEmpty(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

    private string BuildEntityUrl(string tableName, string partitionKey, string rowKey)
        => 
        $"{option.ServiceUri.AbsoluteUri.TrimEnd('/')}/{tableName}"
            + $"(PartitionKey='{Uri.EscapeDataString(partitionKey)}',RowKey='{Uri.EscapeDataString(rowKey)}')";
}
