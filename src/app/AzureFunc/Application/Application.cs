using System;
using System.Linq;
using GarageGroup.Infra;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PrimeFuncPack;

[assembly: RefreshableTokenCredential("0 */30 * * * *", IsDisabled = false)]

namespace GarageGroup.Internal.ExchangeRates;

internal static partial class Application
{
    private static Dependency<IExchangeRateApi> UseExchangeRateApi()
        =>
        PrimaryHandler.UseStandardSocketsHttpHandler()
        .UseLogging("ExchangeRateApi")
        .UsePollyStandard()
        .UseHttpApi()
        .UseExchangeRateApi();

    private static Dependency<IStorageApi> UseStorageApi()
        =>
        PrimaryHandler.UseStandardSocketsHttpHandler()
        .UseLogging("StorageApi")
        .UseTokenCredentialStandard("https://storage.azure.com/.default")
        .UsePollyStandard()
        .UseHttpApi("StorageApi")
        .With(
            ResolveStorageOption)
        .UseStorageApi();

    private static StorageOption ResolveStorageOption(IServiceProvider serviceProvider)
    {
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();

        return new()
        {
            CurrentRatesTableName = configuration.GetRequiredSection("ExchangeRates:Storage:CurrentRatesTableName").Value.OrEmpty(),
            DailyRatesTableName = configuration.GetRequiredSection("ExchangeRates:Storage:DailyRatesTableName").Value.OrEmpty()
        };
    }

    private static CurrentExchangeRateUpdateOption ResolveCurrentExchangeRateUpdateOption(IServiceProvider serviceProvider)
        => 
        new()
        {
            CurrencyPairs = ResolveCurrencyPairs(serviceProvider).Map(
                static pair => new CurrentExchangeRateCurrencyPair
                {
                    BaseCurrency = pair.BaseCurrency,
                    QuoteCurrency = pair.QuoteCurrency
                })
        };

    private static DailyExchangeRateUpdateOption ResolveDailyExchangeRateUpdateOption(IServiceProvider serviceProvider)
        => 
        new()
        {
            CurrencyPairs = ResolveCurrencyPairs(serviceProvider).Map(
                static pair => new DailyExchangeRateCurrencyPair
                {
                    BaseCurrency = pair.BaseCurrency,
                    QuoteCurrency = pair.QuoteCurrency
                })
        };

    private static FlatArray<ConfiguredCurrencyPair> ResolveCurrencyPairs(IServiceProvider serviceProvider)
    {
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        var configuredPairs = configuration["ExchangeRates:CurrencyPairs"];
        var pairValues = string.IsNullOrWhiteSpace(configuredPairs)
            ? configuration.GetSection("ExchangeRates:CurrencyPairs").GetChildren().Select(static section => section.Value)
            : configuredPairs.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        var pairs = pairValues
            .Where(static value => string.IsNullOrWhiteSpace(value) is false)
            .Select(ParseCurrencyPair)
            .ToFlatArray();

        if (pairs.IsEmpty)
        {
            throw new InvalidOperationException("ExchangeRates:CurrencyPairs must contain at least one currency pair.");
        }

        return pairs;
    }

    private static ConfiguredCurrencyPair ParseCurrencyPair(string? value)
    {
        var pair = value.OrEmpty();
        var separatorIndex = pair.IndexOf('/', StringComparison.Ordinal);

        if (separatorIndex <= 0 || separatorIndex == pair.Length - 1 || pair.IndexOf('/', separatorIndex + 1) >= 0)
        {
            throw new InvalidOperationException($"Invalid configured currency pair: '{value}'.");
        }

        var baseCurrency = pair[..separatorIndex].Trim();
        var quoteCurrency = pair[(separatorIndex + 1)..].Trim();

        if (string.IsNullOrEmpty(baseCurrency) || string.IsNullOrEmpty(quoteCurrency))
        {
            throw new InvalidOperationException($"Invalid configured currency pair: '{value}'.");
        }

        return new()
        {
            BaseCurrency = baseCurrency,
            QuoteCurrency = quoteCurrency
        };
    }

    private sealed record class ConfiguredCurrencyPair
    {
        public required string BaseCurrency { get; init; }

        public required string QuoteCurrency { get; init; }
    }
}
