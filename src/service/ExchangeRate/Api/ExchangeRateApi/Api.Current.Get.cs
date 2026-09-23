using System;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra;

namespace GarageGroup.Internal.ExchangeRates;

partial class ExchangeRateApi
{
    public ValueTask<Result<CurrentExchangeRate, Failure<ExchangeRateCurrentGetFailureCode>>> GetCurrentRateAsync(
        ExchangeRateGetIn input, CancellationToken cancellationToken)
        =>
        AsyncPipeline.Pipe(
            input, cancellationToken)
        .Pipe(
            BuildHttpSendIn)
        .ForwardValue(
            httpApi.SendAsync,
            MapCurrentFailure)
        .Forward(
            response => MapCurrentRateOrFailure(response, input));

    private static Result<HttpSendIn, Failure<ExchangeRateCurrentGetFailureCode>> BuildHttpSendIn(
        ExchangeRateGetIn input)
    {
        if (IsInvalidCurrencyPair(input.BaseCurrency, input.QuoteCurrency))
        {
            return Failure.Create(
                ExchangeRateCurrentGetFailureCode.InvalidCurrency,
                "Base and quote currencies must be different three-letter codes.");
        }

        return new HttpSendIn(
            HttpVerb.Get,
            BuildRequestUrl(NormalizeCurrency(input.BaseCurrency), NormalizeCurrency(input.QuoteCurrency)));
    }

    private static Failure<ExchangeRateCurrentGetFailureCode> MapCurrentFailure(HttpSendFailure failure)
        =>
        failure.ToStandardFailure("An unexpected HTTP failure occurred when getting a current exchange rate:")
            .WithFailureCode(ExchangeRateCurrentGetFailureCode.Unknown);

    private static Result<CurrentExchangeRate, Failure<ExchangeRateCurrentGetFailureCode>> MapCurrentRateOrFailure(
        HttpSendOut response, ExchangeRateGetIn input)
        =>
        MapProviderRateOrFailure(
            response,
            NormalizeCurrency(input.BaseCurrency),
            NormalizeCurrency(input.QuoteCurrency))
        .MapFailure(
            static failure => failure.MapFailureCode(MapCurrentProviderFailureCode))
        .MapSuccess(
            providerRate => new CurrentExchangeRate
            {
                BaseCurrency = NormalizeCurrency(input.BaseCurrency),
                QuoteCurrency = NormalizeCurrency(input.QuoteCurrency),
                Rate = providerRate.Rate,
                Source = SourceName,
                FetchedAtUtc = DateTimeOffset.UtcNow,
                ProviderTimestampUtc = new(
                    providerRate.Date.ToDateTime(TimeOnly.MinValue),
                    TimeSpan.Zero)
            });

    private static ExchangeRateCurrentGetFailureCode MapCurrentProviderFailureCode(ProviderRateFailureCode failureCode)
        =>
        failureCode switch
        {
            ProviderRateFailureCode.InvalidCurrency => ExchangeRateCurrentGetFailureCode.InvalidCurrency,
            _ => ExchangeRateCurrentGetFailureCode.Unknown
        };
}
