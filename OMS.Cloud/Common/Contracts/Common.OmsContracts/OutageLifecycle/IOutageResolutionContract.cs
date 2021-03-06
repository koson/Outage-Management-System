﻿using Common.CloudContracts;
using Microsoft.ServiceFabric.Services.Remoting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Common.OmsContracts.OutageLifecycle
{
	[ServiceContract]
	public interface IOutageResolutionContract : IService, IHealthChecker
	{
		[OperationContract]
		Task<bool> ResolveOutage(long outageId);

		[OperationContract]
		Task<bool> ValidateResolveConditions(long outageId);
	}
}
