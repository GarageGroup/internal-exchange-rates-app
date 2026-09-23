namespace GarageGroup.Internal.ExchangeRates;

internal sealed partial class DailyExchangeRateUpdateHandler : IDailyExchangeRateUpdateHandler
{
    private static readonly PipelineParallelOption ParallelOption
        =
        new()
        {
            DegreeOfParallelism = 4,
            FailureAction = PipelineParallelFailureAction.Stop
        };

    private readonly IExchangeRateDailyGetSupplier exchangeRateApi;

    private readonly IDailyExchangeRateStorageSetSupplier storageApi;

    private readonly DailyExchangeRateUpdateOption option;

    internal DailyExchangeRateUpdateHandler(
        IExchangeRateDailyGetSupplier exchangeRateApi,
        IDailyExchangeRateStorageSetSupplier storageApi,
        DailyExchangeRateUpdateOption option)
    {
        this.exchangeRateApi = exchangeRateApi;
        this.storageApi = storageApi;
        this.option = option;
    }
}
