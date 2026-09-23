using System;
using System.Threading;
using System.Threading.Tasks;

namespace GarageGroup.Internal.ExchangeRates;

public interface IDailyExchangeRateStorageSetSupplier
{
    ValueTask<Result<Unit, Failure<StorageFailureCode>>> SetDailyRateAsync(
        DailyExchangeRateStorageSetIn input, CancellationToken cancellationToken);
}
