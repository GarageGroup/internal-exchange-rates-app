namespace GarageGroup.Internal.ExchangeRates;

internal sealed partial class DailyExchangeRateGetHandler : IDailyExchangeRateGetHandler
{
    private readonly IDailyExchangeRateStorageGetSupplier storageApi;

    internal DailyExchangeRateGetHandler(IDailyExchangeRateStorageGetSupplier storageApi)
        => 
        this.storageApi = storageApi;
}
