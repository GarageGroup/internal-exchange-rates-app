using System;
using System.Threading;
using System.Threading.Tasks;

namespace GarageGroup.Internal.ExchangeRates;

public interface ICurrentExchangeRateStorageSetSupplier
{
    ValueTask<Result<Unit, Failure<StorageFailureCode>>> SetCurrentRateAsync(
        CurrentExchangeRateStorageSetIn input, CancellationToken cancellationToken);
}
