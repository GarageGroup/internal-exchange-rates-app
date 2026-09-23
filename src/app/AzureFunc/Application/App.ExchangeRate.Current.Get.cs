using PrimeFuncPack;

namespace GarageGroup.Internal.ExchangeRates;

partial class Application
{
    internal static Dependency<ICurrentExchangeRateGetHandler> UseCurrentExchangeRateGetHandler()
        => 
        UseStorageApi().UseCurrentExchangeRateGetHandler();
}
