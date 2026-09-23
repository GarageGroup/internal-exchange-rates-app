using System;
using System.Threading;
using System.Threading.Tasks;

namespace GarageGroup.Internal.ExchangeRates;

public interface IDailyExchangeRateGetHandler
{
    ValueTask<Result<DailyExchangeRateGetOut, Failure<DailyExchangeRateGetFailureCode>>> HandleAsync(
        DailyExchangeRateGetIn input, CancellationToken cancellationToken);
}
