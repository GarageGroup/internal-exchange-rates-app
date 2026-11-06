namespace GarageGroup.Internal.ExchangeRates;

internal sealed partial class DailyExchangeRateGetHandler : IDailyExchangeRateGetHandler
{
    private readonly IDailyExchangeRateStorageFindSupplier storageApi;

    internal DailyExchangeRateGetHandler(IDailyExchangeRateStorageFindSupplier storageApi)
        => 
        this.storageApi = storageApi;
}
