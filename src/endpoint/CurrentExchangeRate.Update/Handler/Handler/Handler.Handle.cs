using System;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra;

namespace GarageGroup.Internal.ExchangeRates;

partial class CurrentExchangeRateUpdateHandler
{
    public ValueTask<Result<Unit, Failure<HandlerFailureCode>>> HandleAsync(
        Unit input, CancellationToken cancellationToken)
        => 
        AsyncPipeline.Pipe(
            option.CurrencyPairs, cancellationToken)
        .PipeParallelValue(
            UpdateAsync,
            ParallelOption);

    private ValueTask<Result<Unit, Failure<HandlerFailureCode>>> UpdateAsync(
        CurrentExchangeRateCurrencyPair pair, CancellationToken cancellationToken)
        => 
        AsyncPipeline.Pipe(
            pair, cancellationToken)
        .Pipe(
            static pair => new ExchangeRateGetIn
            {
                BaseCurrency = pair.BaseCurrency,
                QuoteCurrency = pair.QuoteCurrency
            })
        .PipeValue(
            exchangeRateApi.GetCurrentRateAsync)
        .MapSuccess(
            static rate => new CurrentExchangeRateStorageSetIn
            {
                BaseCurrency = rate.BaseCurrency,
                QuoteCurrency = rate.QuoteCurrency,
                Rate = rate.Rate,
                Source = rate.Source,
                FetchedAtUtc = rate.FetchedAtUtc,
                ProviderTimestampUtc = rate.ProviderTimestampUtc
            })
        .MapFailure(
            static failure => failure.WithFailureCode(HandlerFailureCode.Transient))
        .ForwardValue(
            storageApi.SetCurrentRateAsync,
            static failure => failure.WithFailureCode(HandlerFailureCode.Transient));
}
