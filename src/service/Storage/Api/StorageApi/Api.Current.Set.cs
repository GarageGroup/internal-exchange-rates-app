using System;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra;

namespace GarageGroup.Internal.ExchangeRates;

partial class StorageApi
{
    public ValueTask<Result<Unit, Failure<StorageFailureCode>>> SetCurrentRateAsync(
        CurrentExchangeRateStorageSetIn input, CancellationToken cancellationToken)
        =>
        AsyncPipeline.Pipe(
            input, cancellationToken)
        .Pipe(
            BuildHttpSendIn)
        .ForwardValue(
            httpApi.SendAsync,
            MapCurrentSetFailure)
        .MapSuccess(
            Unit.From);

    private Result<HttpSendIn, Failure<StorageFailureCode>> BuildHttpSendIn(CurrentExchangeRateStorageSetIn input)
    {
        if (input is null || IsInvalidCurrencyPair(input.BaseCurrency, input.QuoteCurrency) || input.Rate <= 0)
        {
            return Failure.Create(StorageFailureCode.Invalid, "Current exchange rate is invalid.");
        }

        var entity = new CurrentExchangeRateTableEntity
        {
            PartitionKey = NormalizeCurrency(input.BaseCurrency),
            RowKey = NormalizeCurrency(input.QuoteCurrency),
            Rate = FormatRate(input.Rate),
            Source = input.Source,
            FetchedAtUtc = FormatDateTimeOffset(input.FetchedAtUtc),
            ProviderTimestampUtc = input.ProviderTimestampUtc is null ? null : FormatDateTimeOffset(input.ProviderTimestampUtc.Value)
        };

        return new HttpSendIn(HttpVerb.Put, BuildEntityUrl(option.CurrentRatesTableName, entity.PartitionKey, entity.RowKey))
        {
            Headers = SetHeaders,
            Body = HttpBody.SerializeAsJson(entity),
            SuccessType = HttpSuccessType.OnlyStatusCode
        };
    }

    private static Failure<StorageFailureCode> MapCurrentSetFailure(HttpSendFailure failure)
        =>
        failure.ToStandardFailure("An unexpected http failure occurred when setting a current exchange rate:")
        .WithFailureCode(StorageFailureCode.Unknown);
}
