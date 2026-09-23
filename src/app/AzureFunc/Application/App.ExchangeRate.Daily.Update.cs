using PrimeFuncPack;

namespace GarageGroup.Internal.ExchangeRates;

partial class Application
{
    internal static Dependency<IDailyExchangeRateUpdateHandler> UseDailyExchangeRateUpdateHandler()
        => 
        Dependency.Pipe(
            UseExchangeRateApi())
        .With(
            UseStorageApi())
        .With(
            ResolveDailyExchangeRateUpdateOption)
        .UseDailyExchangeRateUpdateHandler();
}
