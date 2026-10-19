using System;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace GarageGroup.Internal.ExchangeRates;

partial class Function
{
    [Function("GetExchangeRatesSwagger")]
    public static async Task<HttpResponseData> GetExchangeRatesSwaggerAsync(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "swagger/swagger.json")] HttpRequestData request,
        CancellationToken cancellationToken)
    {
        using var stream = typeof(Function).Assembly.GetManifestResourceStream("ExchangeRates.OpenApi.json");
        if (stream is null)
        {
            throw new InvalidOperationException("Exchange Rates OpenAPI resource was not found.");
        }

        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
        var response = request.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(document.RootElement, cancellationToken).ConfigureAwait(false);

        return response;
    }
}
