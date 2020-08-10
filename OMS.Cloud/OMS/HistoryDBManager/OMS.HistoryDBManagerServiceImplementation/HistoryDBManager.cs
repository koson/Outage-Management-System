﻿using Common.OMS;
using Common.OMS.OutageDatabaseModel;
using Common.OmsContracts.HistoryDBManager;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Notifications;
using OMS.Common.Cloud;
using OMS.Common.Cloud.Logger;
using OMS.Common.Cloud.ReliableCollectionHelpers;
using OutageDatabase.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OMS.HistoryDBManagerServiceImplementation
{
	public class HistoryDBManager : IHistoryDBManagerContract
    {
        private readonly IReliableStateManager stateManager;
        private UnitOfWork dbContext;
        //TODO: translate to ReliableDictionaryAccess<long, Consumer>
        private ReliableDictionaryAccess<long, long> unenergizedConsumers;
        public ReliableDictionaryAccess<long, long> UnenergizedConsumers
        {
            get
            {
                if(unenergizedConsumers == null)
                {
                    unenergizedConsumers = ReliableDictionaryAccess<long, long>.Create(this.stateManager, ReliableDictionaryNames.UnenergizedConsumers).Result;
                }
                return unenergizedConsumers;
            }
            protected set
            {
                unenergizedConsumers = value;
            }
        }

        //TODO: translate to ReliableDictionaryAccess<long, Switch>
        private ReliableDictionaryAccess<long, long> openedSwitches;
        public ReliableDictionaryAccess<long,long> OpenedSwitches
        {
            get
            {
                if(openedSwitches == null)
                {
                    openedSwitches = ReliableDictionaryAccess<long, long>.Create(this.stateManager, ReliableDictionaryNames.OpenedSwitches).Result;
                }
                return openedSwitches;
            }
            protected set
            {
                openedSwitches = value;
            }
        }
        private ICloudLogger logger;
        private ICloudLogger Logger
        {
            get { return logger ?? (logger = CloudLoggerFactory.GetLogger()); }
        }

        public HistoryDBManager(IReliableStateManager stateManager)
        {
            this.stateManager = stateManager;
            this.stateManager.StateManagerChanged += OnStateManagerChangedHandler;
            this.dbContext = new UnitOfWork();
        }

        private async void OnStateManagerChangedHandler(object sender, NotifyStateManagerChangedEventArgs e)
        {
            if(e.Action == NotifyStateManagerChangedAction.Add)
            {
                var operation = e as NotifyStateManagerSingleEntityChangedEventArgs;
                string reliableStateName = operation.ReliableState.Name.AbsolutePath;
                if (reliableStateName == ReliableDictionaryNames.OpenedSwitches)
                {
                    OpenedSwitches = await ReliableDictionaryAccess<long, long>.Create(this.stateManager, ReliableDictionaryNames.OpenedSwitches);
                }
                else if (reliableStateName == ReliableDictionaryNames.UnenergizedConsumers)
                {
                    UnenergizedConsumers = await ReliableDictionaryAccess<long, long>.Create(this.stateManager, ReliableDictionaryNames.UnenergizedConsumers);
                }
            }
        }

        public async Task OnSwitchClosed(long elementGid)
        {
            try
            {
                if(await OpenedSwitches.ContainsKeyAsync(elementGid))
                {
                    dbContext.EquipmentHistoricalRepository.Add(new EquipmentHistorical() { EquipmentId = elementGid, OperationTime = DateTime.Now, DatabaseOperation = DatabaseOperation.DELETE });
                    await OpenedSwitches.TryRemoveAsync(elementGid);
                    dbContext.Complete();
                }
            }
            catch (Exception ex)
            {
                string message = "HistoryDBManager::OnSwitchClosed method => exception on Complete()";
                Logger.LogError(message, ex);
                Console.WriteLine($"{message}, Message: {ex.Message}, Inner Message: {ex.InnerException.Message})");
            }
        }

        public async Task OnConsumerBlackedOut(List<long> consumers, long? outageId)
        {
            List<ConsumerHistorical> consumerHistoricals = new List<ConsumerHistorical>();
            try
            {
                foreach (var consumer in consumers)
                {
                    if (!await UnenergizedConsumers.ContainsKeyAsync(consumer))
                    {
                        consumerHistoricals.Add(new ConsumerHistorical() { OutageId = outageId, ConsumerId = consumer, OperationTime = DateTime.Now, DatabaseOperation = DatabaseOperation.INSERT });
                        await UnenergizedConsumers.SetAsync(consumer,0);
                    }
                }
                dbContext.ConsumerHistoricalRepository.AddRange(consumerHistoricals);
                dbContext.Complete();
                
            }
            catch (Exception e)
            {
                string message = "HistoryDBManager::OnConsumersBlackedOut method => exception on Complete()";
                Logger.LogError(message, e);
                Console.WriteLine($"{message}, Message: {e.Message}, Inner Message: {e.InnerException.Message})");
            }
        }

        public async Task OnSwitchOpened(long elementGid, long? outageId)
        {
            try
            {
                if (!await OpenedSwitches.ContainsKeyAsync(elementGid))
                {
                    dbContext.EquipmentHistoricalRepository.Add(new EquipmentHistorical() { OutageId = outageId, EquipmentId = elementGid, OperationTime = DateTime.Now, DatabaseOperation = DatabaseOperation.INSERT });
                    await OpenedSwitches.SetAsync(elementGid,0);
                    dbContext.Complete();
                }   
            }
            catch (Exception e)
            {
                string message = "HistoryDBManager::OnSwitchOpened method => exception on Complete()";
                Logger.LogError(message, e);
                Console.WriteLine($"{message}, Message: {e.Message}, Inner Message: {e.InnerException.Message})");
            }
        }

        public async Task OnConsumersEnergized(HashSet<long> consumers)
        {
            List<ConsumerHistorical> consumerHistoricals = new List<ConsumerHistorical>();
            var copy = (await UnenergizedConsumers.GetDataCopyAsync()).Keys.ToList();
            var changedConsumers = copy.Intersect(consumers).ToList();

            foreach (var consumer in changedConsumers)
            {
                consumerHistoricals.Add(new ConsumerHistorical() { ConsumerId = consumer, OperationTime = DateTime.Now, DatabaseOperation = DatabaseOperation.DELETE });
            }

            try
            {
                foreach (var changed in changedConsumers)
                {
                    if (await UnenergizedConsumers.ContainsKeyAsync(changed))
					{
                        await UnenergizedConsumers.TryRemoveAsync(changed);
					}
                }

                dbContext.ConsumerHistoricalRepository.AddRange(consumerHistoricals);
                dbContext.Complete();
                
            }
            catch (Exception e)
            {
                string message = "HistoryDBManager::OnConsumersEnergized method => exception on Complete()";
                Logger.LogError(message, e);
                Console.WriteLine($"{message}, Message: {e.Message}, Inner Message: {e.InnerException.Message})");
            }
        }
    
    }
}
