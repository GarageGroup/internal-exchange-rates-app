using System;
using GarageGroup.Infra;

namespace GarageGroup.Internal.ExchangeRates;

public interface ICurrentExchangeRateUpdateHandler : IHandler<Unit, Unit>;
