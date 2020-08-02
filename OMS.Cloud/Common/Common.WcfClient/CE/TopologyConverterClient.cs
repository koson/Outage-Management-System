﻿using Common.CE.Interfaces;
using Common.CeContracts;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Communication.Wcf.Client;
using OMS.Common.Cloud.Names;
using OMS.Common.PubSub;
using System;
using System.Threading.Tasks;

namespace OMS.Common.WcfClient.CE
{
	public class TopologyConverterClient : WcfSeviceFabricClientBase<ITopologyConverterContract>, ITopologyConverterContract
	{
		private static readonly string microserviceName = MicroserviceNames.TopologyProviderService;
		private static readonly string listenerName = EndpointNames.TopologyConverterServiceEndpoint;

		public TopologyConverterClient(WcfCommunicationClientFactory<ITopologyConverterContract> clientFactory, Uri serviceUri, ServicePartitionKey servicePartition)
			: base(clientFactory, serviceUri, servicePartition, listenerName)
		{

		}

		public static TopologyConverterClient CreateClient()
		{
			ClientFactory factory = new ClientFactory();
			return factory.CreateClient<TopologyConverterClient, ITopologyConverterContract>(microserviceName);
		}

		public static TopologyConverterClient CreateClient(Uri serviceUri, ServicePartitionKey servicePartitionKey)
		{
			ClientFactory factory = new ClientFactory();
			return factory.CreateClient<TopologyConverterClient, ITopologyConverterContract>(serviceUri, servicePartitionKey);
		}

		public Task<IOutageTopologyModel> ConvertTopologyToOMSModel(ITopology topology)
		{
            return InvokeWithRetryAsync(client => client.Channel.ConvertTopologyToOMSModel(topology));
		}

		public Task<UIModel> ConvertTopologyToUIModel(ITopology topology)
		{
            return InvokeWithRetryAsync(client => client.Channel.ConvertTopologyToUIModel(topology));
		}
	}
}
