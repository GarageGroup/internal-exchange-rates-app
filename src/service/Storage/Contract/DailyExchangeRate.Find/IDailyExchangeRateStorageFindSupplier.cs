using System;
using System.Threading;
using System.Threading.Tasks;

namespace GarageGroup.Internal.ExchangeRates;

public interface IDailyExchangeRateStorageFindSupplier
{
    ValueTask<Result<DailyExchangeRateStorageFindOut, Failure<StorageFailureCode>>> FindDailyRateAsync(
        DailyExchangeRateStorageFindIn input, CancellationToken cancellationToken);
}
