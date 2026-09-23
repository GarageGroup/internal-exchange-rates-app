namespace GarageGroup.Internal.ExchangeRates;

internal sealed partial class CurrentExchangeRateGetHandler : ICurrentExchangeRateGetHandler
{
    private readonly ICurrentExchangeRateStorageGetSupplier storageApi;

    internal CurrentExchangeRateGetHandler(ICurrentExchangeRateStorageGetSupplier storageApi)
        => 
        this.storageApi = storageApi;
}
