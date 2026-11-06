using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra;

namespace GarageGroup.Internal.ExchangeRates;

partial class StorageApi
{
    public ValueTask<Result<DailyExchangeRateStorageFindOut, Failure<StorageFailureCode>>> FindDailyRateAsync(
        DailyExchangeRateStorageFindIn input, CancellationToken cancellationToken)
        =>
        AsyncPipeline.Pipe(
            input, cancellationToken)
        .Pipe(
            BuildHttpSendIn)
        .ForwardValue(
            FindDailyRateAsync);

    private Result<HttpSendIn, Failure<StorageFailureCode>> BuildHttpSendIn(DailyExchangeRateStorageFindIn input)
    {
        if (IsInvalidCurrencyPair(input.BaseCurrency, input.QuoteCurrency))
        {
            return Failure.Create(StorageFailureCode.Invalid, "Currency pair is invalid.");
        }

        var firstDate = DateOnly.FromDayNumber(Math.Max(0, input.Date.DayNumber - option.DailyRateLookbackDays));
        var firstPartition = BuildDailyPartitionKey(input.BaseCurrency, firstDate);
        var lastPartition = BuildDailyPartitionKey(input.BaseCurrency, input.Date);
        var quoteCurrency = NormalizeCurrency(input.QuoteCurrency);
        var filter = $"PartitionKey ge '{firstPartition}' and PartitionKey le '{lastPartition}' and RowKey eq '{quoteCurrency}'";

        return new HttpSendIn(HttpVerb.Get, $"{option.DailyRatesTableName}()?$filter={Uri.EscapeDataString(filter)}")
        {
            Headers = GetHeaders
        };
    }

    private async ValueTask<Result<DailyExchangeRateStorageFindOut, Failure<StorageFailureCode>>> FindDailyRateAsync(
        HttpSendIn request, CancellationToken cancellationToken)
    {
        DailyExchangeRateTableEntity? latest = null;
        var queryUrl = request.RequestUri;

        while (true)
        {
            var result = await httpApi.SendAsync(request, cancellationToken).ConfigureAwait(false);
            if (result.IsFailure)
            {
                return MapDailyFindFailure(result.FailureOrThrow());
            }

            var response = result.SuccessOrThrow();
            try
            {
                var page = response.Body.DeserializeFromJson<DailyExchangeRateTablePage>();
                if (page is null)
                {
                    return Failure.Create(StorageFailureCode.Unknown, "Daily exchange rate search response is empty.");
                }

                foreach (var entity in page.Value)
                {
                    if (latest is null || string.CompareOrdinal(entity.PartitionKey, latest.PartitionKey) > 0)
                    {
                        latest = entity;
                    }
                }
            }
            catch (Exception exception)
            {
                return Failure.Create(StorageFailureCode.Unknown, "Daily exchange rate search response is invalid.", exception);
            }

            var nextPartition = GetContinuationHeader(response, "x-ms-continuation-NextPartitionKey");
            var nextRow = GetContinuationHeader(response, "x-ms-continuation-NextRowKey");
            if (string.IsNullOrEmpty(nextPartition) && string.IsNullOrEmpty(nextRow))
            {
                break;
            }

            // Keep the original filter on every page, including after an empty page.
            var nextUrl = queryUrl;
            if (string.IsNullOrEmpty(nextPartition) is false)
            {
                nextUrl += $"&NextPartitionKey={Uri.EscapeDataString(nextPartition)}";
            }

            if (string.IsNullOrEmpty(nextRow) is false)
            {
                nextUrl += $"&NextRowKey={Uri.EscapeDataString(nextRow)}";
            }

            request = new(HttpVerb.Get, nextUrl) { Headers = GetHeaders };
        }

        return latest is null
            ? Failure.Create(StorageFailureCode.NotFound, "Daily exchange rate was not found within the configured lookback period.")
            : MapDailyFoundRateOrFailure(latest);
    }

    private static string? GetContinuationHeader(HttpSendOut response, string name)
        =>
        response.Headers.AsEnumerable()
        .FirstOrDefault(header => string.Equals(header.Key, name, StringComparison.OrdinalIgnoreCase)).Value;

    private static Failure<StorageFailureCode> MapDailyFindFailure(HttpSendFailure failure)
        =>
        failure.ToStandardFailure("An unexpected http failure occurred when searching daily exchange rates:")
        .WithFailureCode(StorageFailureCode.Unknown);

    private static Result<DailyExchangeRateStorageFindOut, Failure<StorageFailureCode>> MapDailyFoundRateOrFailure(
        DailyExchangeRateTableEntity entity)
    {
        try
        {
            return new DailyExchangeRateStorageFindOut
            {
                BaseCurrency = ParseDailyBaseCurrency(entity.PartitionKey),
                QuoteCurrency = entity.RowKey,
                Rate = ParseRate(entity.Rate),
                Source = entity.Source.OrEmpty(),
                EffectiveDate = ParseEffectiveDate(entity.EffectiveDate),
                FetchedAtUtc = ParseDateTimeOffset(entity.FetchedAtUtc)
            };
        }
        catch (Exception exception)
        {
            return Failure.Create(StorageFailureCode.Unknown, "Daily exchange rate response is invalid.", exception);
        }
    }
}
