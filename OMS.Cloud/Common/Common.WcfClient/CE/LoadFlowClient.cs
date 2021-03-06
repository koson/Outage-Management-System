﻿using Common.CeContracts;
using Common.CeContracts.LoadFlow;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Communication.Wcf.Client;
using OMS.Common.Cloud.Names;
using System;
using System.Threading.Tasks;

namespace OMS.Common.WcfClient.CE
{
	public class LoadFlowClient : WcfSeviceFabricClientBase<ILoadFlowContract>, ILoadFlowContract
	{
		private static readonly string microserviceName = MicroserviceNames.CeLoadFlowService;
		private static readonly string listenerName = EndpointNames.CeLoadFlowServiceEndpoint;
		public LoadFlowClient(WcfCommunicationClientFactory<ILoadFlowContract> clientFactory, Uri serviceUri, ServicePartitionKey servicePartition)
			: base(clientFactory, serviceUri, servicePartition, listenerName)
		{

		}

		public static ILoadFlowContract CreateClient()
		{
			ClientFactory factory = new ClientFactory();
			return factory.CreateClient<LoadFlowClient, ILoadFlowContract>(microserviceName);
		}

		public static ILoadFlowContract CreateClient(Uri serviceUri, ServicePartitionKey servicePartitionKey)
		{
			ClientFactory factory = new ClientFactory();
			return factory.CreateClient<LoadFlowClient, ILoadFlowContract>(serviceUri, servicePartitionKey);
		}

		public Task<bool> IsAlive()
		{
			return Task.Run(() => { return true; });
		}

		public Task<TopologyModel> UpdateLoadFlow(TopologyModel topology)
		{
			return InvokeWithRetryAsync(client => client.Channel.UpdateLoadFlow(topology));
		}
	}
}
