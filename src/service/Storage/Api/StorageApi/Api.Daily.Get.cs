using System;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra;

namespace GarageGroup.Internal.ExchangeRates;

partial class StorageApi
{
    public ValueTask<Result<DailyExchangeRateStorageGetOut, Failure<StorageFailureCode>>> GetDailyRateAsync(
        DailyExchangeRateStorageGetIn input, CancellationToken cancellationToken)
        =>
        AsyncPipeline.Pipe(
            input, cancellationToken)
        .Pipe(
            BuildHttpSendIn)
        .ForwardValue(
            httpApi.SendAsync,
            MapDailyGetFailure)
        .Forward(
            MapDailyRateOrFailure);

    private Result<HttpSendIn, Failure<StorageFailureCode>> BuildHttpSendIn(DailyExchangeRateStorageGetIn input)
    {
        if (IsInvalidCurrencyPair(input.BaseCurrency, input.QuoteCurrency))
        {
            return Failure.Create(StorageFailureCode.Invalid, "Currency pair is invalid.");
        }

        var partitionKey = BuildDailyPartitionKey(input.BaseCurrency, input.Date);
        var quoteCurrency = NormalizeCurrency(input.QuoteCurrency);

        return new HttpSendIn(HttpVerb.Get, BuildEntityUrl(option.DailyRatesTableName, partitionKey, quoteCurrency))
        {
            Headers = GetHeaders
        };
    }

    private static Failure<StorageFailureCode> MapDailyGetFailure(HttpSendFailure failure)
        =>
        failure.StatusCode is HttpFailureCode.NotFound
            ? failure.ToStandardFailure("Daily exchange rate was not found:").WithFailureCode(StorageFailureCode.NotFound)
            : failure.ToStandardFailure("An unexpected http failure occurred when getting a daily exchange rate:")
                .WithFailureCode(StorageFailureCode.Unknown);

    private static Result<DailyExchangeRateStorageGetOut, Failure<StorageFailureCode>> MapDailyRateOrFailure(HttpSendOut response)
    {
        try
        {
            var entity = response.Body.DeserializeFromJson<DailyExchangeRateTableEntity>();
            if (entity is null)
            {
                return Failure.Create(StorageFailureCode.Unknown, "Daily exchange rate response is empty.");
            }

            return new DailyExchangeRateStorageGetOut
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
