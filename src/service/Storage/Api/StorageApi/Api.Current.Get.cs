using System;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra;

namespace GarageGroup.Internal.ExchangeRates;

partial class StorageApi
{
    public ValueTask<Result<CurrentExchangeRateStorageGetOut, Failure<StorageFailureCode>>> GetCurrentRateAsync(
        CurrentExchangeRateStorageGetIn input, CancellationToken cancellationToken)
        =>
        AsyncPipeline.Pipe(
            input, cancellationToken)
        .Pipe(
            BuildHttpSendIn)
        .ForwardValue(
            httpApi.SendAsync,
            MapCurrentGetFailure)
        .Forward(
            MapCurrentRateOrFailure);

    private Result<HttpSendIn, Failure<StorageFailureCode>> BuildHttpSendIn(CurrentExchangeRateStorageGetIn input)
    {
        if (IsInvalidCurrencyPair(input.BaseCurrency, input.QuoteCurrency))
        {
            return Failure.Create(StorageFailureCode.Invalid, "Currency pair is invalid.");
        }

        var baseCurrency = NormalizeCurrency(input.BaseCurrency);
        var quoteCurrency = NormalizeCurrency(input.QuoteCurrency);

        return new HttpSendIn(HttpVerb.Get, BuildEntityUrl(option.CurrentRatesTableName, baseCurrency, quoteCurrency))
        {
            Headers = GetHeaders
        };
    }

    private static Failure<StorageFailureCode> MapCurrentGetFailure(HttpSendFailure failure)
        =>
        failure.StatusCode is HttpFailureCode.NotFound
            ? failure.ToStandardFailure("Current exchange rate was not found:").WithFailureCode(StorageFailureCode.NotFound)
            : failure.ToStandardFailure("An unexpected http failure occurred when getting a current exchange rate:")
                .WithFailureCode(StorageFailureCode.Unknown);

    private static Result<CurrentExchangeRateStorageGetOut, Failure<StorageFailureCode>> MapCurrentRateOrFailure(HttpSendOut response)
    {
        try
        {
            var entity = response.Body.DeserializeFromJson<CurrentExchangeRateTableEntity>();
            if (entity is null)
            {
                return Failure.Create(StorageFailureCode.Unknown, "Current exchange rate response is empty.");
            }

            return new CurrentExchangeRateStorageGetOut
            {
                BaseCurrency = entity.PartitionKey,
                QuoteCurrency = entity.RowKey,
                Rate = ParseRate(entity.Rate),
                Source = entity.Source.OrEmpty(),
                FetchedAtUtc = ParseDateTimeOffset(entity.FetchedAtUtc),
                ProviderTimestampUtc = string.IsNullOrEmpty(entity.ProviderTimestampUtc)
                    ? null
                    : ParseDateTimeOffset(entity.ProviderTimestampUtc)
            };
        }
        catch (Exception exception)
        {
            return Failure.Create(StorageFailureCode.Unknown, "Current exchange rate response is invalid.", exception);
        }
    }
}
