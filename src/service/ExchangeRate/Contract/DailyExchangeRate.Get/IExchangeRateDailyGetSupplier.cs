using System;
using System.Threading;
using System.Threading.Tasks;

namespace GarageGroup.Internal.ExchangeRates;

public interface IExchangeRateDailyGetSupplier
{
    ValueTask<Result<DailyExchangeRate, Failure<ExchangeRateDailyGetFailureCode>>> GetDailyRateAsync(
        DailyExchangeRateGetIn input, CancellationToken cancellationToken);
}
