using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Hosting;

namespace GarageGroup.Internal.ExchangeRates;

internal static class Program
{
    private static Task Main(string[] args)
        => FunctionsApplication.CreateBuilder(args).Build().RunAsync();
}
