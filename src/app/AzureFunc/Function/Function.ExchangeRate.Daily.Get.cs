using System;
using System.Globalization;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace GarageGroup.Internal.ExchangeRates;

partial class Function
{
    [Function("GetDailyExchangeRate")]
    public static async Task<HttpResponseData> GetDailyExchangeRateAsync(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "exchange-rates/daily/{baseCurrency}/{quoteCurrency}/{date}")] HttpRequestData request,
        string baseCurrency,
        string quoteCurrency,
        string date,
        CancellationToken cancellationToken)
    {
        var handler = Application.UseDailyExchangeRateGetHandler().Resolve(request.FunctionContext.InstanceServices);
        if (DateOnly.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var effectiveDate) is false)
        {
            var invalidResponse = request.CreateResponse(HttpStatusCode.BadRequest);
            await invalidResponse.WriteAsJsonAsync(new { error = "Date must have yyyy-MM-dd format." }, cancellationToken).ConfigureAwait(false);
            return invalidResponse;
        }

        var result = await handler.HandleAsync(
            new() { BaseCurrency = baseCurrency, QuoteCurrency = quoteCurrency, Date = effectiveDate },
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
            DailyExchangeRateGetFailureCode.Invalid => HttpStatusCode.BadRequest,
            DailyExchangeRateGetFailureCode.NotFound => HttpStatusCode.NotFound,
            _ => HttpStatusCode.InternalServerError
        });
        await failureResponse.WriteAsJsonAsync(new { error = failure.FailureMessage }, cancellationToken).ConfigureAwait(false);
        
        return failureResponse;
    }
}
