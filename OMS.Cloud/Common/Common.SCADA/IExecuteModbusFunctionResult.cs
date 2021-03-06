﻿using OMS.Common.PubSubContracts.DataContracts.SCADA;
using System.Collections.Generic;

namespace OMS.Common.SCADA
{
    public interface IExecuteModbusFunctionResult
    {
    }

    public interface IReadAnalogResult : IExecuteModbusFunctionResult
    {
        Dictionary<long, AnalogModbusData> Data { get; }
    }

    public interface IReadDiscreteResult : IExecuteModbusFunctionResult
    {
        Dictionary<long, DiscreteModbusData> Data { get; }
    }
}
