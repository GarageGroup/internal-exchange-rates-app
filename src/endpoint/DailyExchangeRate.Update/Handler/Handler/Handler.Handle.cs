using System;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra;

namespace GarageGroup.Internal.ExchangeRates;

partial class DailyExchangeRateUpdateHandler
{
    public ValueTask<Result<Unit, Failure<HandlerFailureCode>>> HandleAsync(
        Unit input, CancellationToken cancellationToken)
    {
        var effectiveDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1);

        return AsyncPipeline.Pipe(
            option.CurrencyPairs, cancellationToken)
        .PipeParallelValue(
            (pair, token) => UpdateAsync(pair, effectiveDate, token),
            ParallelOption);
    }

    private ValueTask<Result<Unit, Failure<HandlerFailureCode>>> UpdateAsync(
        DailyExchangeRateCurrencyPair pair, DateOnly effectiveDate, CancellationToken cancellationToken)
        => 
        AsyncPipeline.Pipe(
            pair, cancellationToken)
        .Pipe(
            pair => new DailyExchangeRateGetIn
            {
                BaseCurrency = pair.BaseCurrency,
                QuoteCurrency = pair.QuoteCurrency,
                Date = effectiveDate
            })
        .PipeValue(
            exchangeRateApi.GetDailyRateAsync)
        .MapSuccess(
            static rate => new DailyExchangeRateStorageSetIn
            {
                BaseCurrency = rate.BaseCurrency,
                QuoteCurrency = rate.QuoteCurrency,
                Rate = rate.Rate,
                Source = rate.Source,
                EffectiveDate = rate.EffectiveDate,
                FetchedAtUtc = rate.FetchedAtUtc
            })
        .MapFailure(
            static failure => failure.WithFailureCode(HandlerFailureCode.Transient))
        .ForwardValue(
            storageApi.SetDailyRateAsync,
            static failure => failure.WithFailureCode(HandlerFailureCode.Transient));
}
