﻿using Common.PubSub;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Common.PubSubContracts.DataContracts
{
    [DataContract]
    [KnownType(typeof(SingleAnalogValueSCADAMessage))]
    [KnownType(typeof(MultipleAnalogValueSCADAMessage))]
    [KnownType(typeof(SingleDiscreteValueSCADAMessage))]
    [KnownType(typeof(MultipleDiscreteValueSCADAMessage))]
    public abstract class SCADAMessage : IPublishableMessage
    {
    }

    [DataContract]
    public class SingleAnalogValueSCADAMessage : SCADAMessage
    {
        public SingleAnalogValueSCADAMessage(AnalogModbusData analogModbusData)
        {
            AnalogModbusData = analogModbusData;
        }

        [DataMember]
        public AnalogModbusData AnalogModbusData { get; private set; }
    }

    [DataContract]
    public class MultipleAnalogValueSCADAMessage : SCADAMessage
    {
        public MultipleAnalogValueSCADAMessage(Dictionary<long, AnalogModbusData> data)
        {
            Data = data;
        }

        [DataMember]
        public Dictionary<long, AnalogModbusData> Data { get; private set; }
    }

    [DataContract]
    public class SingleDiscreteValueSCADAMessage : SCADAMessage
    {
        public SingleDiscreteValueSCADAMessage(DiscreteModbusData discreteModbusData)
        {
            DiscreteModbusData = discreteModbusData;
        }

        [DataMember]
        public DiscreteModbusData DiscreteModbusData { get; private set; }
    }

    [DataContract]
    public class MultipleDiscreteValueSCADAMessage : SCADAMessage
    {
        public MultipleDiscreteValueSCADAMessage(Dictionary<long, DiscreteModbusData> data)
        {
            Data = data;
        }

        [DataMember]
        public Dictionary<long, DiscreteModbusData> Data { get; private set; }
    }
}
