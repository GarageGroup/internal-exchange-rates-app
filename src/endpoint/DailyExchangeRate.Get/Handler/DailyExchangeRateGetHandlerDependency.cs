using System;
using PrimeFuncPack;

namespace GarageGroup.Internal.ExchangeRates;

public static class DailyExchangeRateGetHandlerDependency
{
    public static Dependency<IDailyExchangeRateGetHandler> UseDailyExchangeRateGetHandler<TStorageApi>(
        this Dependency<TStorageApi> dependency)
        where TStorageApi : IDailyExchangeRateStorageGetSupplier
    {
        ArgumentNullException.ThrowIfNull(dependency);
        return dependency.Map<IDailyExchangeRateGetHandler>(CreateHandler);

        static DailyExchangeRateGetHandler CreateHandler(TStorageApi storageApi)
        {
            ArgumentNullException.ThrowIfNull(storageApi);
            return new(storageApi);
        }
    }
}
