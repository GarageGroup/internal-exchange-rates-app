using PrimeFuncPack;

namespace GarageGroup.Internal.ExchangeRates;

partial class Application
{
    internal static Dependency<IDailyExchangeRateGetHandler> UseDailyExchangeRateGetHandler()
        => 
        UseStorageApi().UseDailyExchangeRateGetHandler();
}
