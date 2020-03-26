﻿using Microsoft.ServiceFabric.Services.Remoting;
using Outage.Common.GDA;
using System.Collections.Generic;
using System.ServiceModel;

namespace Outage.Common.ServiceContracts.DistributedTransaction
{
    [ServiceContract]
    public interface IModelUpdateNotificationContract : IService
    {
        [OperationContract]
        bool NotifyAboutUpdate(Dictionary<DeltaOpType, List<long>> modelChanges);
    }
}
