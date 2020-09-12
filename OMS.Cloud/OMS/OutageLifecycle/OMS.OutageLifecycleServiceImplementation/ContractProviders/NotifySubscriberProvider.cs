﻿using Common.OMS;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Notifications;
using OMS.Common.Cloud.Logger;
using OMS.Common.Cloud.Names;
using OMS.Common.Cloud.ReliableCollectionHelpers;
using OMS.Common.PubSubContracts;
using OMS.Common.PubSubContracts.DataContracts.SCADA;
using OMS.Common.PubSubContracts.Interfaces;
using System;
using System.Threading.Tasks;

namespace OMS.OutageLifecycleImplementation.ContractProviders
{
    public class NotifySubscriberProvider : INotifySubscriberContract
	{
		private const string subscriberName = MicroserviceNames.OmsOutageLifecycleService;

		private readonly string baseLogString;
		private readonly IReliableStateManager stateManager;

		private ICloudLogger logger;
		private ICloudLogger Logger
		{
			get { return logger ?? (logger = CloudLoggerFactory.GetLogger()); }
		}

		#region Reliable Dictionaries
		private bool isMonitoredHeadBreakerMeasurementsInitialized;

		private bool ReliableDictionariesInitialized
		{
			get
			{
				return isMonitoredHeadBreakerMeasurementsInitialized;
			}
		}

		private ReliableDictionaryAccess<long, DiscreteModbusData> monitoredHeadBreakerMeasurements;
		private ReliableDictionaryAccess<long, DiscreteModbusData> MonitoredHeadBreakerMeasurements
		{
			get { return monitoredHeadBreakerMeasurements; }
		}

		private async void OnStateManagerChangedHandler(object sender, NotifyStateManagerChangedEventArgs e)
		{
			if (e.Action == NotifyStateManagerChangedAction.Add)
			{
				var operation = e as NotifyStateManagerSingleEntityChangedEventArgs;
				string reliableStateName = operation.ReliableState.Name.AbsolutePath;

				if (reliableStateName == ReliableDictionaryNames.MonitoredHeadBreakerMeasurements)
				{
					this.monitoredHeadBreakerMeasurements = await ReliableDictionaryAccess<long, DiscreteModbusData>.Create(stateManager, ReliableDictionaryNames.MonitoredHeadBreakerMeasurements);
					this.isMonitoredHeadBreakerMeasurementsInitialized = true;

					string debugMessage = $"{baseLogString} OnStateManagerChangedHandler => '{ReliableDictionaryNames.MonitoredHeadBreakerMeasurements}' ReliableDictionaryAccess initialized.";
					Logger.LogDebug(debugMessage);
				}
			}
		}
        #endregion Reliable Dictionaries

		public NotifySubscriberProvider(IReliableStateManager stateManager)
		{
			this.baseLogString = $"{this.GetType()} [{this.GetHashCode()}] =>{Environment.NewLine}";

			this.isMonitoredHeadBreakerMeasurementsInitialized = false;

			this.stateManager = stateManager;
			this.stateManager.StateManagerChanged += this.OnStateManagerChangedHandler;
		}

		#region INotifySubscriberContract
		public Task<string> GetSubscriberName()
		{
			return Task.Run(() => subscriberName);
		}

		public async Task Notify(IPublishableMessage message, string publisherName)
		{
			Logger.LogDebug($"{baseLogString} Notify method started.");

			while (!ReliableDictionariesInitialized)
            {
				await Task.Delay(1000);
            }

			if (!(message is MultipleDiscreteValueSCADAMessage multipleDiscreteValueSCADAMessage))
            {
				Logger.LogWarning($"{baseLogString} Notify => unexpected type of message: {message.GetType()}");
				return;
            }

            try
            {
				var discreteData = multipleDiscreteValueSCADAMessage.Data;

				var enumerableHeadBreakerMeasurements = await MonitoredHeadBreakerMeasurements.GetEnumerableDictionaryAsync();

				foreach (var headMeasurementGid in enumerableHeadBreakerMeasurements.Keys)
				{
					if (!discreteData.ContainsKey(headMeasurementGid))
					{
						continue;
					}

					await MonitoredHeadBreakerMeasurements.SetAsync(headMeasurementGid, discreteData[headMeasurementGid]);
				}
			}
            catch (Exception e)
            {
				string errorMessage = $"{baseLogString} Notify => Exception: {e.Message}";
				Logger.LogError(errorMessage, e);
			}
		}

		public Task<bool> IsAlive()
		{
			return Task.Run(() => true);
		}
		#endregion INotifySubscriberContract
	}
}