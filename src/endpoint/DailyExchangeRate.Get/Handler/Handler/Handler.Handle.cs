using System;
using System.Threading;
using System.Threading.Tasks;

namespace GarageGroup.Internal.ExchangeRates;

partial class DailyExchangeRateGetHandler
{
    public ValueTask<Result<DailyExchangeRateGetOut, Failure<DailyExchangeRateGetFailureCode>>> HandleAsync(
        DailyExchangeRateGetIn input, CancellationToken cancellationToken)
        =>
        AsyncPipeline.Pipe(
            input, cancellationToken)
        .Pipe(
            static input => new DailyExchangeRateStorageFindIn
            {
                BaseCurrency = input.BaseCurrency,
                QuoteCurrency = input.QuoteCurrency,
                Date = input.Date
            })
        .PipeValue(
            storageApi.FindDailyRateAsync)
        .Map(
            static rate => new DailyExchangeRateGetOut
            {
                BaseCurrency = rate.BaseCurrency,
                QuoteCurrency = rate.QuoteCurrency,
                Rate = rate.Rate,
                Source = rate.Source,
                EffectiveDate = rate.EffectiveDate,
                FetchedAtUtc = rate.FetchedAtUtc
            },
            static failure => failure.MapFailureCode(MapFailureCode));

    private static DailyExchangeRateGetFailureCode MapFailureCode(StorageFailureCode failureCode)
        =>
        failureCode switch
        {
            StorageFailureCode.Invalid => DailyExchangeRateGetFailureCode.Invalid,
            StorageFailureCode.NotFound => DailyExchangeRateGetFailureCode.NotFound,
            _ => DailyExchangeRateGetFailureCode.Unknown
        };
}
