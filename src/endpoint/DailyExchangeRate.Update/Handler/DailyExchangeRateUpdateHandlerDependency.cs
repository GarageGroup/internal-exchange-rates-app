using System;
using PrimeFuncPack;

namespace GarageGroup.Internal.ExchangeRates;

public static class DailyExchangeRateUpdateHandlerDependency
{
    public static Dependency<IDailyExchangeRateUpdateHandler> UseDailyExchangeRateUpdateHandler<TExchangeRateApi, TStorageApi>(
        this Dependency<TExchangeRateApi, TStorageApi, DailyExchangeRateUpdateOption> dependency)
        where TExchangeRateApi : IExchangeRateDailyGetSupplier
        where TStorageApi : IDailyExchangeRateStorageSetSupplier
    {
        ArgumentNullException.ThrowIfNull(dependency);
        return dependency.Fold<IDailyExchangeRateUpdateHandler>(CreateHandler);

        static DailyExchangeRateUpdateHandler CreateHandler(
            TExchangeRateApi exchangeRateApi, TStorageApi storageApi, DailyExchangeRateUpdateOption option)
        {
            ArgumentNullException.ThrowIfNull(exchangeRateApi);
            ArgumentNullException.ThrowIfNull(storageApi);
            ArgumentNullException.ThrowIfNull(option);

            return new(exchangeRateApi, storageApi, option);
        }
    }
}
