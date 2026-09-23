using System;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra;

namespace GarageGroup.Internal.ExchangeRates;

partial class StorageApi
{
    public ValueTask<Result<Unit, Failure<StorageFailureCode>>> SetDailyRateAsync(
        DailyExchangeRateStorageSetIn input, CancellationToken cancellationToken)
        =>
        AsyncPipeline.Pipe(
            input, cancellationToken)
        .Pipe(
            BuildHttpSendIn)
        .ForwardValue(
            httpApi.SendAsync,
            MapDailySetFailure)
        .MapSuccess(
            Unit.From);

    private Result<HttpSendIn, Failure<StorageFailureCode>> BuildHttpSendIn(DailyExchangeRateStorageSetIn input)
    {
        if (input is null || IsInvalidCurrencyPair(input.BaseCurrency, input.QuoteCurrency) || input.Rate <= 0)
        {
            return Failure.Create(StorageFailureCode.Invalid, "Daily exchange rate is invalid.");
        }

        var entity = new DailyExchangeRateTableEntity
        {
            PartitionKey = BuildDailyPartitionKey(input.BaseCurrency, input.EffectiveDate),
            RowKey = NormalizeCurrency(input.QuoteCurrency),
            Rate = FormatRate(input.Rate),
            Source = input.Source,
            EffectiveDate = input.EffectiveDate.ToString(EffectiveDateFormat, System.Globalization.CultureInfo.InvariantCulture),
            FetchedAtUtc = FormatDateTimeOffset(input.FetchedAtUtc)
        };

        return new HttpSendIn(HttpVerb.Put, BuildEntityUrl(option.DailyRatesTableName, entity.PartitionKey, entity.RowKey))
        {
            Headers = SetHeaders,
            Body = HttpBody.SerializeAsJson(entity),
            SuccessType = HttpSuccessType.OnlyStatusCode
        };
    }

    private static Failure<StorageFailureCode> MapDailySetFailure(HttpSendFailure failure)
        =>
        failure.ToStandardFailure("An unexpected http failure occurred when setting a daily exchange rate:")
        .WithFailureCode(StorageFailureCode.Unknown);
}
