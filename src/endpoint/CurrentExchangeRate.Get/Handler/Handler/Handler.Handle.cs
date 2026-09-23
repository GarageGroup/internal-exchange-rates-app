using System;
using System.Threading;
using System.Threading.Tasks;

namespace GarageGroup.Internal.ExchangeRates;

partial class CurrentExchangeRateGetHandler
{
    public ValueTask<Result<CurrentExchangeRateGetOut, Failure<CurrentExchangeRateGetFailureCode>>> HandleAsync(
        CurrentExchangeRateGetIn input, CancellationToken cancellationToken)
        =>
        AsyncPipeline.Pipe(
            input, cancellationToken)
        .Pipe(
            static input => new CurrentExchangeRateStorageGetIn
            {
                BaseCurrency = input.BaseCurrency,
                QuoteCurrency = input.QuoteCurrency
            })
        .PipeValue(
            storageApi.GetCurrentRateAsync)
        .Map(
            static rate => new CurrentExchangeRateGetOut
            {
                BaseCurrency = rate.BaseCurrency,
                QuoteCurrency = rate.QuoteCurrency,
                Rate = rate.Rate,
                Source = rate.Source,
                FetchedAtUtc = rate.FetchedAtUtc
            },
            static failure => failure.MapFailureCode(MapFailureCode));

    private static CurrentExchangeRateGetFailureCode MapFailureCode(StorageFailureCode failureCode)
        => 
        failureCode switch
        {
            StorageFailureCode.Invalid => CurrentExchangeRateGetFailureCode.Invalid,
            StorageFailureCode.NotFound => CurrentExchangeRateGetFailureCode.NotFound,
            _ => CurrentExchangeRateGetFailureCode.Unknown
        };
}
