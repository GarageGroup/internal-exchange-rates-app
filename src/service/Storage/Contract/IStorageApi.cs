namespace GarageGroup.Internal.ExchangeRates;

public interface IStorageApi :
    ICurrentExchangeRateStorageGetSupplier,
    ICurrentExchangeRateStorageSetSupplier,
    IDailyExchangeRateStorageGetSupplier,
    IDailyExchangeRateStorageSetSupplier;
