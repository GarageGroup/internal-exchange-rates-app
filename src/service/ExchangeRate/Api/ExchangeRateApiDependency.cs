using System;
using GarageGroup.Infra;
using PrimeFuncPack;

namespace GarageGroup.Internal.ExchangeRates;

public static class ExchangeRateApiDependency
{
    public static Dependency<IExchangeRateApi> UseExchangeRateApi(this Dependency<IHttpApi> dependency)
    {
        ArgumentNullException.ThrowIfNull(dependency);
        return dependency.Map<IExchangeRateApi>(CreateApi);

        static ExchangeRateApi CreateApi(IHttpApi httpApi)
        {
            ArgumentNullException.ThrowIfNull(httpApi);
            return new(httpApi);
        }
    }
}
