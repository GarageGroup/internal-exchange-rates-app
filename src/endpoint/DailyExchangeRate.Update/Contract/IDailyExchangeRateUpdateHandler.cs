using System;
using GarageGroup.Infra;

namespace GarageGroup.Internal.ExchangeRates;

public interface IDailyExchangeRateUpdateHandler : IHandler<Unit, Unit>;
