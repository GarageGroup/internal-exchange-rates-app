using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra;
using Microsoft.Azure.Functions.Worker;

namespace GarageGroup.Internal.ExchangeRates;

partial class Function
{
    [Function("UpdateDailyExchangeRates")]
    public static Task UpdateDailyExchangeRatesAsync(
        [TimerTrigger("%ExchangeRates:DailyUpdateSchedule%")] JsonElement timerInfo,
        FunctionContext context,
        CancellationToken cancellationToken)
        => 
        Application.UseDailyExchangeRateUpdateHandler()
            .RunAzureFunctionAsync<IDailyExchangeRateUpdateHandler, Unit, Unit>(timerInfo, context, cancellationToken);
}
