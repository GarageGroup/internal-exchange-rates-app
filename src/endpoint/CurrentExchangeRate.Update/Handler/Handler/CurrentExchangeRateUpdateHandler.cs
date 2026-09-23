namespace GarageGroup.Internal.ExchangeRates;

internal sealed partial class CurrentExchangeRateUpdateHandler : ICurrentExchangeRateUpdateHandler
{
    private static readonly PipelineParallelOption ParallelOption
        =
        new()
        {
            DegreeOfParallelism = 4,
            FailureAction = PipelineParallelFailureAction.Stop
        };

    private readonly IExchangeRateCurrentGetSupplier exchangeRateApi;

    private readonly ICurrentExchangeRateStorageSetSupplier storageApi;

    private readonly CurrentExchangeRateUpdateOption option;

    internal CurrentExchangeRateUpdateHandler(
        IExchangeRateCurrentGetSupplier exchangeRateApi,
        ICurrentExchangeRateStorageSetSupplier storageApi,
        CurrentExchangeRateUpdateOption option)
    {
        this.exchangeRateApi = exchangeRateApi;
        this.storageApi = storageApi;
        this.option = option;
    }
}
