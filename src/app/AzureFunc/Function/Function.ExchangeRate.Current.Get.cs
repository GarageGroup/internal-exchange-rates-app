using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace GarageGroup.Internal.ExchangeRates;

partial class Function
{
    [Function("GetCurrentExchangeRate")]
    public static async Task<HttpResponseData> GetCurrentExchangeRateAsync(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "exchange-rates/current/{baseCurrency}/{quoteCurrency}")] HttpRequestData request,
        string baseCurrency,
        string quoteCurrency,
        CancellationToken cancellationToken)
    {
        var handler = Application.UseCurrentExchangeRateGetHandler().Resolve(request.FunctionContext.InstanceServices);
        var result = await handler.HandleAsync(
            new() { BaseCurrency = baseCurrency, QuoteCurrency = quoteCurrency },
            cancellationToken).ConfigureAwait(false);

        if (result.IsSuccess)
        {
            var response = request.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(result.SuccessOrThrow(), cancellationToken).ConfigureAwait(false);
            return response;
        }

        var failure = result.FailureOrThrow();
        var failureResponse = request.CreateResponse(failure.FailureCode switch
        {
            CurrentExchangeRateGetFailureCode.Invalid => HttpStatusCode.BadRequest,
            CurrentExchangeRateGetFailureCode.NotFound => HttpStatusCode.NotFound,
            _ => HttpStatusCode.InternalServerError
        });
        await failureResponse.WriteAsJsonAsync(new { error = failure.FailureMessage }, cancellationToken).ConfigureAwait(false);
        
        return failureResponse;
    }
}
