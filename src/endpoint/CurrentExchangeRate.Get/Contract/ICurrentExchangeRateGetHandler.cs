using System;
using System.Threading;
using System.Threading.Tasks;

namespace GarageGroup.Internal.ExchangeRates;

public interface ICurrentExchangeRateGetHandler
{
    ValueTask<Result<CurrentExchangeRateGetOut, Failure<CurrentExchangeRateGetFailureCode>>> HandleAsync(
        CurrentExchangeRateGetIn input, CancellationToken cancellationToken);
}
