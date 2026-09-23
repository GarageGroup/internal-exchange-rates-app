using System;
using System.Threading;
using System.Threading.Tasks;

namespace GarageGroup.Internal.ExchangeRates;

public interface IDailyExchangeRateStorageGetSupplier
{
    ValueTask<Result<DailyExchangeRateStorageGetOut, Failure<StorageFailureCode>>> GetDailyRateAsync(
        DailyExchangeRateStorageGetIn input, CancellationToken cancellationToken);
}
