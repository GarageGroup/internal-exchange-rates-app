using System;
using PrimeFuncPack;

namespace GarageGroup.Internal.ExchangeRates;

public static class CurrentExchangeRateUpdateHandlerDependency
{
    public static Dependency<ICurrentExchangeRateUpdateHandler> UseCurrentExchangeRateUpdateHandler<TExchangeRateApi, TStorageApi>(
        this Dependency<TExchangeRateApi, TStorageApi, CurrentExchangeRateUpdateOption> dependency)
        where TExchangeRateApi : IExchangeRateCurrentGetSupplier
        where TStorageApi : ICurrentExchangeRateStorageSetSupplier
    {
        ArgumentNullException.ThrowIfNull(dependency);
        return dependency.Fold<ICurrentExchangeRateUpdateHandler>(CreateHandler);

        static CurrentExchangeRateUpdateHandler CreateHandler(
            TExchangeRateApi exchangeRateApi, TStorageApi storageApi, CurrentExchangeRateUpdateOption option)
        {
            ArgumentNullException.ThrowIfNull(exchangeRateApi);
            ArgumentNullException.ThrowIfNull(storageApi);
            ArgumentNullException.ThrowIfNull(option);

            return new(exchangeRateApi, storageApi, option);
        }
    }
}
