﻿using System.Collections.Generic;
using System.ServiceModel;

namespace Common.CE.Interfaces
{
    //[ServiceKnownType(typeof(EnergyConsumer))]
    //[ServiceKnownType(typeof(Feeder))]
    //[ServiceKnownType(typeof(Field))]
    //[ServiceKnownType(typeof(Recloser))]
    //[ServiceKnownType(typeof(SynchronousMachine))]
    //[ServiceKnownType(typeof(TopologyElement))]
    public interface ITopologyElement : IGraphElement
    {
        long Id { get; set; }
        string Description { get; set; }
        string Mrid { get; set; }
        string Name { get; set; }
        ITopologyElement FirstEnd { get; set; }
        List<ITopologyElement> SecondEnd { get; set; }
        string DmsType { get; set; }
        bool IsRemote { get; set; }
        bool IsActive { get; set; }
        float NominalVoltage { get; set; }
        Dictionary<long, string> Measurements { get; set; }
        bool NoReclosing { get; set; }
        ITopologyElement Feeder { get; set; }
    }
}
