﻿using Common.CeContracts;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Communication.Wcf.Client;
using OMS.Common.Cloud;
using OMS.Common.Cloud.Names;
using OMS.Common.PubSubContracts.DataContracts.SCADA;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OMS.Common.WcfClient.CE
{
	public class MeasurementProviderClient : WcfSeviceFabricClientBase<IMeasurementProviderContract>, IMeasurementProviderContract
	{
		private static readonly string microserviceName = MicroserviceNames.CeMeasurementProviderService;
		private static readonly string listenerName = EndpointNames.CeMeasurementProviderEndpoint;
		public MeasurementProviderClient(WcfCommunicationClientFactory<IMeasurementProviderContract> clientFactory, Uri serviceUri, ServicePartitionKey servicePartition)
			: base(clientFactory, serviceUri, servicePartition, listenerName)
		{

		}

		public Task<bool> IsAlive()
		{
			return Task.Run(() => { return true; });
		}

		public static IMeasurementProviderContract CreateClient()
		{
			ClientFactory factory = new ClientFactory();
			return factory.CreateClient<MeasurementProviderClient, IMeasurementProviderContract>(microserviceName);
		}

		public static IMeasurementProviderContract CreateClient(Uri serviceUri, ServicePartitionKey servicePartitionKey)
		{
			ClientFactory factory = new ClientFactory();
			return factory.CreateClient<MeasurementProviderClient, IMeasurementProviderContract>(serviceUri, servicePartitionKey);
		}

		public Task AddAnalogMeasurement(AnalogMeasurement analogMeasurement)
		{
			return InvokeWithRetryAsync(client => client.Channel.AddAnalogMeasurement(analogMeasurement));

		}

		public Task AddDiscreteMeasurement(DiscreteMeasurement discreteMeasurement)
		{
			return InvokeWithRetryAsync(client => client.Channel.AddDiscreteMeasurement(discreteMeasurement));
		}

		public Task AddMeasurementElementPair(long measurementId, long elementId)
		{
			return InvokeWithRetryAsync(client => client.Channel.AddMeasurementElementPair(measurementId, elementId));
		}

		public Task CommitTransaction()
		{
			return InvokeWithRetryAsync(client => client.Channel.CommitTransaction());
		}

		public Task<float> GetAnalogValue(long measurementGid)
		{
			return InvokeWithRetryAsync(client => client.Channel.GetAnalogValue(measurementGid));
		}

		public Task<bool> GetDiscreteValue(long measurementGid)
		{
			return InvokeWithRetryAsync(client => client.Channel.GetDiscreteValue(measurementGid));
		}

		public Task<long> GetElementGidForMeasurement(long measurementGid)
		{
			return InvokeWithRetryAsync(client => client.Channel.GetElementGidForMeasurement(measurementGid));
		}

		public Task<Dictionary<long, List<long>>> GetElementToMeasurementMap()
		{
			return InvokeWithRetryAsync(client => client.Channel.GetElementToMeasurementMap());
		}

		public Task<List<long>> GetMeasurementsOfElement(long elementGid)
		{
			return InvokeWithRetryAsync(client => client.Channel.GetMeasurementsOfElement(elementGid));
		}

		public Task<Dictionary<long, long>> GetMeasurementToElementMap()
		{
			return InvokeWithRetryAsync(client => client.Channel.GetMeasurementToElementMap());
		}

		public Task<bool> PrepareForTransaction()
		{
			return InvokeWithRetryAsync(client => client.Channel.PrepareForTransaction());
		}

		public Task RollbackTransaction()
		{
			return InvokeWithRetryAsync(client => client.Channel.RollbackTransaction());
		}

		public Task<AnalogMeasurement> GetAnalogMeasurement(long measurementGid)
		{
			return InvokeWithRetryAsync(client => client.Channel.GetAnalogMeasurement(measurementGid));
		}

		public Task<DiscreteMeasurement> GetDiscreteMeasurement(long measurementGid)
		{
			return InvokeWithRetryAsync(client => client.Channel.GetDiscreteMeasurement(measurementGid));
		}

		public Task UpdateAnalogMeasurement(Dictionary<long, AnalogModbusData> data)
		{
			return InvokeWithRetryAsync(client => client.Channel.UpdateAnalogMeasurement(data));
		}

		public Task UpdateDiscreteMeasurement(Dictionary<long, DiscreteModbusData> data)
		{
			return InvokeWithRetryAsync(client => client.Channel.UpdateDiscreteMeasurement(data));
		}

        public Task<bool> SendSingleAnalogCommand(long measurementGid, float commandingValue, CommandOriginType commandOrigin)
        {
			return InvokeWithRetryAsync(client => client.Channel.SendSingleAnalogCommand(measurementGid, commandingValue, commandOrigin));
		}

        public Task<bool> SendMultipleAnalogCommand(Dictionary<long, float> commands, CommandOriginType commandOrigin)
        {
			return InvokeWithRetryAsync(client => client.Channel.SendMultipleAnalogCommand(commands, commandOrigin));
		}

        public Task<bool> SendSingleDiscreteCommand(long measurementGid, int value, CommandOriginType commandOrigin)
        {
			return InvokeWithRetryAsync(client => client.Channel.SendSingleDiscreteCommand(measurementGid, value, commandOrigin));
		}

        public Task<bool> SendMultipleDiscreteCommand(Dictionary<long, int> commands, CommandOriginType commandOrigin)
        {
			return InvokeWithRetryAsync(client => client.Channel.SendMultipleDiscreteCommand(commands, commandOrigin));
		}
    }
}
