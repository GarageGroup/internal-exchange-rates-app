using System;
using PrimeFuncPack;

namespace GarageGroup.Internal.ExchangeRates;

public static class CurrentExchangeRateGetHandlerDependency
{
    public static Dependency<ICurrentExchangeRateGetHandler> UseCurrentExchangeRateGetHandler<TStorageApi>(
        this Dependency<TStorageApi> dependency)
        where TStorageApi : ICurrentExchangeRateStorageGetSupplier
    {
        ArgumentNullException.ThrowIfNull(dependency);
        return dependency.Map<ICurrentExchangeRateGetHandler>(CreateHandler);

        static CurrentExchangeRateGetHandler CreateHandler(TStorageApi storageApi)
        {
            ArgumentNullException.ThrowIfNull(storageApi);
            return new(storageApi);
        }
    }
}
