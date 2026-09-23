using System;
using System.Threading;
using System.Threading.Tasks;

namespace GarageGroup.Internal.ExchangeRates;

public interface IExchangeRateCurrentGetSupplier
{
    ValueTask<Result<CurrentExchangeRate, Failure<ExchangeRateCurrentGetFailureCode>>> GetCurrentRateAsync(
        ExchangeRateGetIn input, CancellationToken cancellationToken);
}
