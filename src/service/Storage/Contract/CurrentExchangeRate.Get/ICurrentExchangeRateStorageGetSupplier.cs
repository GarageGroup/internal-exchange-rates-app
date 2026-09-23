using System;
using System.Threading;
using System.Threading.Tasks;

namespace GarageGroup.Internal.ExchangeRates;

public interface ICurrentExchangeRateStorageGetSupplier
{
    ValueTask<Result<CurrentExchangeRateStorageGetOut, Failure<StorageFailureCode>>> GetCurrentRateAsync(
        CurrentExchangeRateStorageGetIn input, CancellationToken cancellationToken);
}
