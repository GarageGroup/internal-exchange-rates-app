using PrimeFuncPack;

namespace GarageGroup.Internal.ExchangeRates;

partial class Application
{
    internal static Dependency<ICurrentExchangeRateUpdateHandler> UseCurrentExchangeRateUpdateHandler()
        => 
        Dependency.Pipe(
            UseExchangeRateApi())
        .With(
            UseStorageApi())
        .With(
            ResolveCurrentExchangeRateUpdateOption)
        .UseCurrentExchangeRateUpdateHandler();
}
