﻿using Common.CloudContracts;
using Microsoft.ServiceFabric.Services.Remoting;
using OMS.Common.SCADA;
using OMS.Common.ScadaContracts.DataContracts.ModbusFunctions;
using System.ServiceModel;
using System.Threading.Tasks;

namespace OMS.Common.ScadaContracts.FunctionExecutior
{
    [ServiceContract]
    public interface IWriteCommandEnqueuerContract : IService, IHealthChecker
    {
        [OperationContract]
        [ServiceKnownType(typeof(ReadFunction))]
        [ServiceKnownType(typeof(WriteSingleFunction))]
        [ServiceKnownType(typeof(WriteMultipleFunction))]
        Task<bool> EnqueueWriteCommand(IWriteModbusFunction modbusFunction);
    }
}
