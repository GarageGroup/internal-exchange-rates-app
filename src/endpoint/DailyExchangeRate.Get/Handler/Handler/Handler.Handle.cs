using System;
using System.Threading;
using System.Threading.Tasks;

namespace GarageGroup.Internal.ExchangeRates;

partial class DailyExchangeRateGetHandler
{
    public async ValueTask<Result<DailyExchangeRateGetOut, Failure<DailyExchangeRateGetFailureCode>>> HandleAsync(
        DailyExchangeRateGetIn input, CancellationToken cancellationToken)
    {
        var date = input.Date;

        while (date > DateOnly.MinValue)
        {
            var result = await storageApi.GetDailyRateAsync(
                new()
                {
                    BaseCurrency = input.BaseCurrency,
                    QuoteCurrency = input.QuoteCurrency,
                    Date = date
                },
                cancellationToken).ConfigureAwait(false);

            if (result.IsSuccess)
            {
                var rate = result.SuccessOrThrow();

                return new DailyExchangeRateGetOut
                {
                    BaseCurrency = rate.BaseCurrency,
                    QuoteCurrency = rate.QuoteCurrency,
                    Rate = rate.Rate,
                    Source = rate.Source,
                    EffectiveDate = rate.EffectiveDate,
                    FetchedAtUtc = rate.FetchedAtUtc
                };
            }

            var failure = result.FailureOrThrow();
            if (failure.FailureCode is not StorageFailureCode.NotFound)
            {
                return failure.MapFailureCode(MapFailureCode);
            }

            date = date.AddDays(-1);
        }

        return Failure.Create(
            DailyExchangeRateGetFailureCode.NotFound,
            $"Daily exchange rate '{input.BaseCurrency}/{input.QuoteCurrency}' was not found on or before '{input.Date:yyyy-MM-dd}'.");
    }

    private static DailyExchangeRateGetFailureCode MapFailureCode(StorageFailureCode failureCode)
        => 
        failureCode switch
        {
            StorageFailureCode.Invalid => DailyExchangeRateGetFailureCode.Invalid,
            StorageFailureCode.NotFound => DailyExchangeRateGetFailureCode.NotFound,
            _ => DailyExchangeRateGetFailureCode.Unknown
        };
}
