using System;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra;

namespace GarageGroup.Internal.ExchangeRates;

partial class ExchangeRateApi
{
    public ValueTask<Result<DailyExchangeRate, Failure<ExchangeRateDailyGetFailureCode>>> GetDailyRateAsync(
        DailyExchangeRateGetIn input, CancellationToken cancellationToken)
        =>
        AsyncPipeline.Pipe(
            input, cancellationToken)
        .Pipe(
            BuildHttpSendIn)
        .ForwardValue(
            httpApi.SendAsync,
            MapDailyFailure)
        .Forward(
            response => MapDailyRateOrFailure(response, input));

    private static Result<HttpSendIn, Failure<ExchangeRateDailyGetFailureCode>> BuildHttpSendIn(
        DailyExchangeRateGetIn input)
    {
        if (IsInvalidCurrencyPair(input.BaseCurrency, input.QuoteCurrency))
        {
            return Failure.Create(
                ExchangeRateDailyGetFailureCode.InvalidCurrency,
                "Base and quote currencies must be different three-letter codes.");
        }

        return new HttpSendIn(
            HttpVerb.Get,
            BuildRequestUrl(NormalizeCurrency(input.BaseCurrency), NormalizeCurrency(input.QuoteCurrency), input.Date));
    }

    private static Failure<ExchangeRateDailyGetFailureCode> MapDailyFailure(HttpSendFailure failure)
        =>
        failure.ToStandardFailure("An unexpected HTTP failure occurred when getting a daily exchange rate:")
            .WithFailureCode(ExchangeRateDailyGetFailureCode.Unknown);

    private static Result<DailyExchangeRate, Failure<ExchangeRateDailyGetFailureCode>> MapDailyRateOrFailure(
        HttpSendOut response, DailyExchangeRateGetIn input)
        =>
        MapProviderRateOrFailure(
            response,
            NormalizeCurrency(input.BaseCurrency),
            NormalizeCurrency(input.QuoteCurrency))
        .MapFailure(
            static failure => failure.MapFailureCode(MapDailyProviderFailureCode))
        .MapSuccess(
            providerRate => new DailyExchangeRate
            {
                BaseCurrency = NormalizeCurrency(input.BaseCurrency),
                QuoteCurrency = NormalizeCurrency(input.QuoteCurrency),
                Rate = providerRate.Rate,
                Source = SourceName,
                EffectiveDate = providerRate.Date,
                FetchedAtUtc = DateTimeOffset.UtcNow
            });

    private static ExchangeRateDailyGetFailureCode MapDailyProviderFailureCode(ProviderRateFailureCode failureCode)
        =>
        failureCode switch
        {
            ProviderRateFailureCode.InvalidCurrency => ExchangeRateDailyGetFailureCode.InvalidCurrency,
            _ => ExchangeRateDailyGetFailureCode.Unknown
        };
}
