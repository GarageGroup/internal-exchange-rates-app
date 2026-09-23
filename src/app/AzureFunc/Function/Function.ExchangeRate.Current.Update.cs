using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra;
using Microsoft.Azure.Functions.Worker;

namespace GarageGroup.Internal.ExchangeRates;

partial class Function
{
    [Function("UpdateCurrentExchangeRates")]
    public static Task UpdateCurrentExchangeRatesAsync(
        [TimerTrigger("%ExchangeRates:CurrentUpdateSchedule%")] JsonElement timerInfo,
        FunctionContext context,
        CancellationToken cancellationToken)
        => 
        Application.UseCurrentExchangeRateUpdateHandler()
            .RunAzureFunctionAsync<ICurrentExchangeRateUpdateHandler, Unit, Unit>(timerInfo, context, cancellationToken);
}
