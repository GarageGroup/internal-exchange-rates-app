using System;
using System.Globalization;
using System.Text.Json.Serialization;
using GarageGroup.Infra;

namespace GarageGroup.Internal.ExchangeRates;

internal sealed partial class ExchangeRateApi(IHttpApi httpApi) : IExchangeRateApi
{
    private const string SourceName = "Frankfurter";

    private const string DateFormat = "yyyy-MM-dd";

    private const string BaseUrl = "https://api.frankfurter.dev/v2/rate";

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

    private static string BuildRequestUrl(string baseCurrency, string quoteCurrency, DateOnly? date = null)
        => 
        date is null
            ? $"{BaseUrl}/{baseCurrency}/{quoteCurrency}"
            : $"{BaseUrl}/{baseCurrency}/{quoteCurrency}?date={date.Value.ToString(DateFormat, CultureInfo.InvariantCulture)}";

    private static Result<ProviderRate, Failure<ProviderRateFailureCode>> MapProviderRateOrFailure(
        HttpSendOut response, string baseCurrency, string quoteCurrency)
    {
        try
        {
            var json = response.Body.DeserializeFromJson<ProviderRateJson>();
            if (json is null || DateOnly.TryParseExact(json.Date, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) is false)
            {
                return Failure.Create(ProviderRateFailureCode.Unknown, "Exchange rate provider response is invalid.");
            }

            if (string.Equals(json.BaseCurrency, baseCurrency, StringComparison.OrdinalIgnoreCase) is false
                || string.Equals(json.QuoteCurrency, quoteCurrency, StringComparison.OrdinalIgnoreCase) is false)
            {
                return Failure.Create(
                    ProviderRateFailureCode.InvalidCurrency,
                    $"Exchange rate '{baseCurrency}/{quoteCurrency}' is absent in the provider response.");
            }

            if (json.Rate <= 0)
            {
                return Failure.Create(ProviderRateFailureCode.Unknown, "Exchange rate provider response contains an invalid rate.");
            }

            return new ProviderRate(json.Rate, date);
        }
        catch (Exception exception)
        {
            return Failure.Create(ProviderRateFailureCode.Unknown, "Exchange rate provider response is invalid.", exception);
        }
    }

    private enum ProviderRateFailureCode
    {
        Unknown,

        InvalidCurrency
    }

    private readonly record struct ProviderRate(decimal Rate, DateOnly Date);

    private sealed record class ProviderRateJson
    {
        [JsonPropertyName("date")]
        public string? Date { get; init; }

        [JsonPropertyName("base")]
        public string? BaseCurrency { get; init; }

        [JsonPropertyName("quote")]
        public string? QuoteCurrency { get; init; }

        [JsonPropertyName("rate")]
        public decimal Rate { get; init; }
    }
}
