﻿using Common.OMS;
using Common.OmsContracts.DataContracts.OutageDatabaseModel;
using Common.OmsContracts.HistoryDBManager;
using Common.OmsContracts.OutageLifecycle;
using Common.PubSubContracts.DataContracts.CE;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Notifications;
using OMS.Common.Cloud;
using OMS.Common.Cloud.Logger;
using OMS.Common.Cloud.ReliableCollectionHelpers;
using OMS.Common.WcfClient.CE;
using OMS.Common.WcfClient.OMS.HistoryDBManager;
using OMS.Common.WcfClient.OMS.ModelAccess;
using OMS.OutageLifecycleImplementation.Algorithm;
using OMS.OutageLifecycleImplementation.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OMS.OutageLifecycleImplementation.ContractProviders
{
    public class PotentialOutageReportingProvider : IPotentialOutageReportingContract
    {
        private readonly string baseLogString;
        private readonly OutageLifecycleHelper lifecycleHelper;
        private readonly OutageMessageMapper outageMessageMapper;
        private readonly HashSet<CommandOriginType> ignorableCommandOriginTypes;
        private readonly IReliableStateManager stateManager;

		private ICloudLogger logger;

        private ICloudLogger Logger
        {
            get { return logger ?? (logger = CloudLoggerFactory.GetLogger()); }
        }

        #region Reliable Dictionaries
        private bool isStartedIsolationAlgorithmsInitialized;
        private bool isRecloserOutageMapInitialized;
        private bool isOutageTopologyModelInitialized;
        private bool isOptimumIsolationPointsInitialized;
        private bool isCommandedElementsInitialized;
        private bool isPotentialOutagesQueueInitialized;

        private bool ReliableDictionariesInitialized
		{
			get
			{
				return isStartedIsolationAlgorithmsInitialized &&
                       isRecloserOutageMapInitialized &&
                       isOutageTopologyModelInitialized &&
                       isOptimumIsolationPointsInitialized &&
                       isCommandedElementsInitialized &&
                       isPotentialOutagesQueueInitialized;
			}
		}

        private ReliableDictionaryAccess<long, IsolationAlgorithm> startedIsolationAlgorithms;
        private ReliableDictionaryAccess<long, IsolationAlgorithm> StartedIsolationAlgorithms
        {
            get { return startedIsolationAlgorithms; }
        }

        private ReliableDictionaryAccess<long, Dictionary<long, List<long>>> recloserOutageMap;
        private ReliableDictionaryAccess<long, Dictionary<long, List<long>>> RecloserOutageMap
        {
            get { return recloserOutageMap; }
        }

        private ReliableDictionaryAccess<string, OutageTopologyModel> outageTopologyModel;
        private ReliableDictionaryAccess<string, OutageTopologyModel> OutageTopologyModel
        {
            get { return outageTopologyModel; }
        }

        /// <summary>
        /// KEY - element gid of optimum isolation point
        /// VALUE - element gid of head switch (to identify the corresponding algorithm)
        /// </summary>
        private ReliableDictionaryAccess<long, long> optimumIsolationPoints;
        private ReliableDictionaryAccess<long, long> OptimumIsolationPoints
        {
            get { return optimumIsolationPoints; }
        }

        private ReliableDictionaryAccess<long, CommandedElement> commandedElements;
        private ReliableDictionaryAccess<long, CommandedElement> CommandedElements
        {
            get { return commandedElements; }
        }

        private ReliableQueueAccess<PotentialOutageCommand> potentialOutagesQueue;
        private ReliableQueueAccess<PotentialOutageCommand> PotentialOutagesQueue
        {
            get { return potentialOutagesQueue; }
        }

        private async void OnStateManagerChangedHandler(object sender, NotifyStateManagerChangedEventArgs e)
		{
			if (e.Action == NotifyStateManagerChangedAction.Add)
			{
				var operation = e as NotifyStateManagerSingleEntityChangedEventArgs;
				string reliableStateName = operation.ReliableState.Name.AbsolutePath;
                
                if (reliableStateName == ReliableDictionaryNames.StartedIsolationAlgorithms)
                {
                    this.startedIsolationAlgorithms = await ReliableDictionaryAccess<long, IsolationAlgorithm>.Create(stateManager, ReliableDictionaryNames.StartedIsolationAlgorithms);
                    this.isStartedIsolationAlgorithmsInitialized = true;

                    string debugMessage = $"{baseLogString} OnStateManagerChangedHandler => '{ReliableDictionaryNames.StartedIsolationAlgorithms}' ReliableDictionaryAccess initialized.";
                    Logger.LogDebug(debugMessage);
                }
                else if (reliableStateName == ReliableDictionaryNames.RecloserOutageMap)
                {
                    this.recloserOutageMap = await ReliableDictionaryAccess<long, Dictionary<long, List<long>>>.Create(stateManager, ReliableDictionaryNames.RecloserOutageMap);
                    this.isRecloserOutageMapInitialized = true;

                    string debugMessage = $"{baseLogString} OnStateManagerChangedHandler => '{ReliableDictionaryNames.RecloserOutageMap}' ReliableDictionaryAccess initialized.";
                    Logger.LogDebug(debugMessage);
                }
                else if (reliableStateName == ReliableDictionaryNames.OutageTopologyModel)
                {
                    this.outageTopologyModel = await ReliableDictionaryAccess<string, OutageTopologyModel>.Create(stateManager, ReliableDictionaryNames.OutageTopologyModel);
                    this.isOutageTopologyModelInitialized = true;

                    string debugMessage = $"{baseLogString} OnStateManagerChangedHandler => '{ReliableDictionaryNames.OutageTopologyModel}' ReliableDictionaryAccess initialized.";
                    Logger.LogDebug(debugMessage);
                }
                else if (reliableStateName == ReliableDictionaryNames.OptimumIsolationPoints)
                {
                    this.optimumIsolationPoints = await ReliableDictionaryAccess<long, long>.Create(stateManager, ReliableDictionaryNames.OptimumIsolationPoints);
                    this.isOptimumIsolationPointsInitialized = true;

                    string debugMessage = $"{baseLogString} OnStateManagerChangedHandler => '{ReliableDictionaryNames.OptimumIsolationPoints}' ReliableDictionaryAccess initialized.";
                    Logger.LogDebug(debugMessage);
                }
                else if (reliableStateName == ReliableDictionaryNames.CommandedElements)
                {
                    this.commandedElements = await ReliableDictionaryAccess<long, CommandedElement>.Create(stateManager, ReliableDictionaryNames.CommandedElements);
                    this.isCommandedElementsInitialized = true;

                    string debugMessage = $"{baseLogString} OnStateManagerChangedHandler => '{ReliableDictionaryNames.CommandedElements}' ReliableDictionaryAccess initialized.";
                    Logger.LogDebug(debugMessage);
                }
                else if (reliableStateName == ReliableQueueNames.PotentialOutages)
                {
                    this.potentialOutagesQueue = await ReliableQueueAccess<PotentialOutageCommand>.Create(stateManager, ReliableQueueNames.PotentialOutages);
                    this.isPotentialOutagesQueueInitialized = true;

                    string debugMessage = $"{baseLogString} OnStateManagerChangedHandler => '{ReliableQueueNames.PotentialOutages}' ReliableDictionaryAccess initialized.";
                    Logger.LogDebug(debugMessage);
                }
            }
		}
		#endregion Reliable Dictionaries

		public PotentialOutageReportingProvider(IReliableStateManager stateManager, OutageLifecycleHelper outageLifecycleHelper)
        {
            this.baseLogString = $"{this.GetType()} [{this.GetHashCode()}] =>{Environment.NewLine}";

			this.outageMessageMapper = new OutageMessageMapper();
            this.lifecycleHelper = outageLifecycleHelper;

            this.ignorableCommandOriginTypes = new HashSet<CommandOriginType>()
            {
                CommandOriginType.USER_COMMAND,
                CommandOriginType.ISOLATING_ALGORITHM_COMMAND,
                CommandOriginType.UNKNOWN_ORIGIN,
            };

            this.isStartedIsolationAlgorithmsInitialized = false;
            this.isRecloserOutageMapInitialized = false;
            this.isOutageTopologyModelInitialized = false;
            this.isPotentialOutagesQueueInitialized = false;
            this.isCommandedElementsInitialized = false;
            this.isOptimumIsolationPointsInitialized = false;

            this.stateManager = stateManager;
			this.stateManager.StateManagerChanged += this.OnStateManagerChangedHandler;
		}

        #region IPotentialOutageReportingContract
        public async Task<bool> EnqueuePotentialOutageCommand(long elementGid, CommandOriginType commandOriginType, NetworkType networkType)
        {
            Logger.LogDebug($"{baseLogString} EnqueuePotentialOutageCommand method started.");

            while (!ReliableDictionariesInitialized)
            {
                await Task.Delay(1000);
            }

            try
            {
                var command = new PotentialOutageCommand()
                {
                    ElementGid = elementGid,
                    CommandOriginType = commandOriginType,
                    NetworkType = networkType,
                };

                await PotentialOutagesQueue.EnqueueAsync(command);
                return true;
            }
            catch (Exception e)
            {
                string message = "EnqueuePotentialOutageCommand => exception caught";
                Logger.LogError(message, e);
                return false;
            }
        }

        public async Task<bool> ReportPotentialOutage(long elementGid, CommandOriginType commandOriginType, NetworkType networkType)
        {
            Logger.LogVerbose($"{baseLogString} ReportPotentialOutage method started. ElementGid: 0x{elementGid:X16}, CommandOriginType: {commandOriginType}, NetworkType: {networkType}");

            while(!ReliableDictionariesInitialized)
            {
                await Task.Delay(1000);
            }

            try
            {
                #region Preconditions
                var ceModelProviderClient = CeModelProviderClient.CreateClient();
                if (await ceModelProviderClient.IsRecloser(elementGid))
                {
                    Logger.LogWarning($"{baseLogString} ReportPotentialOutage => Element with gid 0x{elementGid:X16} is a Recloser. Call to ReportPotentialOutage aborted.");
                    return false;
                }

                var enumerableTopology = await OutageTopologyModel.GetEnumerableDictionaryAsync();

                if (!enumerableTopology.ContainsKey(ReliableDictionaryNames.OutageTopologyModel))
                {
                    Logger.LogError($"{baseLogString} ReportPotentialOutage => Topology not found in Rel Dictionary: {ReliableDictionaryNames.OutageTopologyModel}.");
                    return false;
                }

                var topology = enumerableTopology[ReliableDictionaryNames.OutageTopologyModel];
                var affectedConsumersGids = GetAffectedConsumers(elementGid, topology, networkType);

                var historyDBManagerClient = HistoryDBManagerClient.CreateClient();

                if (!(await CheckPreconditions(elementGid, commandOriginType, affectedConsumersGids, historyDBManagerClient)))
                {
                    Logger.LogWarning($"{baseLogString} ReportPotentialOutage => Parameters do not satisfy required preconditions. ElementId: 0x{elementGid:X16}, CommandOriginType: {commandOriginType}");
                    return false;
                }
                #endregion Preconditions

                Logger.LogInformation($"{baseLogString} ReportPotentialOutage => Reporting outage for gid: 0x{elementGid:X16}, CommandOriginType: {commandOriginType}");

                var result = await StoreActiveOutage(elementGid, affectedConsumersGids, topology);

                if (!result.HasValue)
                {
                    Logger.LogError($"{baseLogString} ReportPotentialOutage => Storing outage on element 0x{elementGid:X16} FAILED.");
                    return false;
                }

                var createdOutage = result.Value;
                Logger.LogInformation($"{baseLogString} ReportPotentialOutage => Outage on element with gid: 0x{createdOutage.OutageElementGid:x16} is successfully stored in database.");

                await historyDBManagerClient.OnSwitchOpened(elementGid, createdOutage.OutageId);
                await historyDBManagerClient.OnConsumerBlackedOut(affectedConsumersGids, createdOutage.OutageId);

                return await lifecycleHelper.PublishOutageAsync(Topic.ACTIVE_OUTAGE, outageMessageMapper.MapOutageEntity(createdOutage));
            }
            catch (Exception e)
            {
                string message = $"{baseLogString} ReportPotentialOutage =>  exception: {e.Message}";
                Logger.LogError(message, e);
                return false;
            }
        }

        public Task<bool> IsAlive()
        {
            return Task.Run(() => { return true; });
        }
        #endregion IPotentialOutageReportingContract

        #region Private Methods
        private List<long> GetAffectedConsumers(long potentialOutageGid, OutageTopologyModel topology, NetworkType networkType)
        {
            List<long> affectedConsumers = new List<long>();
            Stack<long> nodesToBeVisited = new Stack<long>();
            HashSet<long> visited = new HashSet<long>();
            long startingSwitch = potentialOutageGid;

            //TODO: pogledati kad se bude testirala NoScada
            //if(networkType == NetworkType.NON_SCADA_NETWORK)
            //{
                //TODO: cemu sluzi ova logika? -deluje da podize starter ka gore.... a cemu to sluzi boga pitaj, eventualno za neSkada deo, bolje razdvojiti metode...
                if (topology.OutageTopology.TryGetValue(potentialOutageGid, out OutageTopologyElement firstElement)
                    && topology.OutageTopology.TryGetValue(firstElement.FirstEnd, out OutageTopologyElement currentElementAbove))
                {
                    while (!currentElementAbove.DmsType.Equals("ENERGYSOURCE"))
                    {
                        if (currentElementAbove.IsOpen)
                        {
                            startingSwitch = currentElementAbove.Id;
                            break;
                        }

                        if (!topology.OutageTopology.TryGetValue(currentElementAbove.FirstEnd, out currentElementAbove))
                        {
                            break;
                        }
                    }
                }
            //}


            nodesToBeVisited.Push(startingSwitch);

            while (nodesToBeVisited.Count > 0)
            {
                long currentNode = nodesToBeVisited.Pop();

                if (!visited.Contains(currentNode))
                {
                    visited.Add(currentNode);

                    if (!topology.OutageTopology.TryGetValue(currentNode, out OutageTopologyElement topologyElement))
                    {
                        //TOOD
                        string message = $"GID: 0x{currentNode:X16} not found in topologyModel.OutageTopology dictionary....";
                        Logger.LogError(message);

                        continue; //or throw? //break
                    }

                    foreach (long adjNode in topologyElement.SecondEnd)
                    {
                        nodesToBeVisited.Push(adjNode);
                    }

                    if (topologyElement.DmsType != "ENERGYCONSUMER")
                    {
                        continue;
                    }

                    if(networkType == NetworkType.SCADA_NETWORK && !topologyElement.IsActive)
                    {
                        affectedConsumers.Add(currentNode);
                    }
                    else if (networkType == NetworkType.SCADA_NETWORK && !topologyElement.IsRemote)
                    {
                        affectedConsumers.Add(currentNode);
                    }
                }
            }

            return affectedConsumers;
        }

        private List<Consumer> GetAffectedConsumersFromDatabase(List<long> affectedConsumersIds)
        {
            var consumerAccessClient = ConsumerAccessClient.CreateClient();
            List<Consumer> affectedConsumers = new List<Consumer>();

            foreach (long affectedConsumerId in affectedConsumersIds)
            {
                Consumer affectedConsumer = consumerAccessClient.GetConsumer(affectedConsumerId).Result;

                if (affectedConsumer == null)
                {
                    break;
                }

                affectedConsumers.Add(affectedConsumer);
            }

            return affectedConsumers;
        }

        private async Task OnZeroAffectedConsumersCase(long elementGid, IHistoryDBManagerContract historyDBManagerClient)
        {
            bool isSwitchInvoked = false;

            var enumerableRecloserOutageMap = await RecloserOutageMap.GetEnumerableDictionaryAsync();

            if (enumerableRecloserOutageMap.TryGetValue(elementGid, out Dictionary<long, List<long>> outageAffectedPair))
            {
                foreach (var pair in outageAffectedPair)
                {
                    await historyDBManagerClient.OnConsumerBlackedOut(pair.Value, pair.Key);
                    await historyDBManagerClient.OnSwitchOpened(elementGid, pair.Key);
                    isSwitchInvoked = true;
                }
            }

            if (!isSwitchInvoked)
            {
                await historyDBManagerClient.OnSwitchOpened(elementGid, null);
            }
        }

        private async Task<bool> CheckPreconditions(long elementGid, CommandOriginType commandOriginType, List<long> affectedConsumersGids, IHistoryDBManagerContract historyDBManagerClient)
        {
            if (this.ignorableCommandOriginTypes.Contains(commandOriginType))
            {
                Logger.LogDebug($"{baseLogString} CheckPreconditions => ignorable command origin type: {commandOriginType}");
                return false;
            }

            var enumerableStartedAlgorithms = await StartedIsolationAlgorithms.GetEnumerableDictionaryAsync();
            var enumerableOptimumIsolationPoints = await OptimumIsolationPoints.GetEnumerableDictionaryAsync();

            if (enumerableStartedAlgorithms.Values.Any(algorithm => algorithm.ElementsCommandedInCurrentCycle.Contains(elementGid)) || enumerableOptimumIsolationPoints.ContainsKey(elementGid))
            {
                Logger.LogWarning($"{baseLogString} CheckPreconditions => ElementGid 0x{elementGid:X16} found in elements commanded in current isolating algorithm cycle or in optimumIsolationPoints.");
                return false;
            }

            if (affectedConsumersGids.Count == 0)
            {
                await OnZeroAffectedConsumersCase(elementGid, historyDBManagerClient);

                Logger.LogWarning($"{baseLogString} ReportPotentialOutage => There is no affected consumers => outage report is not valid. ElementGid: 0x{elementGid:X16}, CommandOriginType: {commandOriginType}");
                return false;
            }

            var outageModelAccessClient = OutageModelAccessClient.CreateClient();
            var activeOutages = await outageModelAccessClient.GetAllActiveOutages();
            
            if(activeOutages.Any(active => (active.OutageState == OutageState.CREATED && active.OutageElementGid == elementGid) ||
                                           (active.OutageState != OutageState.CREATED && active.DefaultIsolationPoints.Any(point => point.EquipmentId == elementGid))))
            {
                Logger.LogWarning($"{baseLogString} ReportPotentialOutage => duplicate... ElementGID: 0x{elementGid:X16}");
                return false;
            }

            return true;
        }

        private async Task<ConditionalValue<OutageEntity>> StoreActiveOutage(long elementGid, List<long> affectedConsumersGids, OutageTopologyModel topology)
        {
            var outageModelAccessClient = OutageModelAccessClient.CreateClient();
            var allOutages = await outageModelAccessClient.GetAllOutages();
            var targetedOutages = allOutages.Where(outage => outage.OutageElementGid == elementGid && outage.OutageState != OutageState.ARCHIVED);

            if (targetedOutages.FirstOrDefault() != null)
            {
                Logger.LogWarning($"{baseLogString} StoreActiveOutage => Malfunction on element with gid: 0x{elementGid:x16} has already been reported.");
                return new ConditionalValue<OutageEntity>(false, null);
            }

            List<Consumer> consumerDbEntities = GetAffectedConsumersFromDatabase(affectedConsumersGids);

            if (consumerDbEntities.Count != affectedConsumersGids.Count)
            {
                Logger.LogWarning($"{baseLogString} StoreActiveOutage => Some of affected consumers are not present in database.");
                return new ConditionalValue<OutageEntity>(false, null);
            }

            long recloserId = GetRecloserForHeadBreaker(elementGid, topology);

            List<Equipment> defaultIsolationPoints = await lifecycleHelper.GetEquipmentEntityAsync(new List<long> { elementGid, recloserId });

            OutageEntity createdActiveOutage = new OutageEntity
            {
                OutageElementGid = elementGid,
                AffectedConsumers = consumerDbEntities,
                OutageState = OutageState.CREATED,
                ReportTime = DateTime.UtcNow,
                DefaultIsolationPoints = defaultIsolationPoints,
            };

            var activeOutageDbEntity = await outageModelAccessClient.AddOutage(createdActiveOutage);

            if (activeOutageDbEntity == null)
            {
                Logger.LogError($"{baseLogString} StoreActiveOutage => activeOutageDbEntity is null.");
                return new ConditionalValue<OutageEntity>(false, null);
            }

            await UpdateRecloserOutageMap(recloserId, affectedConsumersGids, activeOutageDbEntity);

            return new ConditionalValue<OutageEntity>(true, activeOutageDbEntity);
        }

        private async Task UpdateRecloserOutageMap(long recloserId, List<long> affectedConsumersGids, OutageEntity createdActiveOutage)
        {
            var enumerableRecloserOutageMap = await RecloserOutageMap.GetEnumerableDictionaryAsync();

            if (enumerableRecloserOutageMap.TryGetValue(recloserId, out Dictionary<long, List<long>> outageToAffectedConsumersMap))
            {
                if (outageToAffectedConsumersMap.ContainsKey(createdActiveOutage.OutageId))
                {
                    outageToAffectedConsumersMap[createdActiveOutage.OutageId] = new List<long>(affectedConsumersGids);
                }
                else
                {
                    outageToAffectedConsumersMap.Add(createdActiveOutage.OutageId, affectedConsumersGids);
                }

                await RecloserOutageMap.SetAsync(recloserId, outageToAffectedConsumersMap);
            }
            else
            {
                var outageIdToAffectedConsumersMap = new Dictionary<long, List<long>>()
                {
                    { createdActiveOutage.OutageId, affectedConsumersGids }
                };

                await RecloserOutageMap.SetAsync(recloserId, outageIdToAffectedConsumersMap);
            }
        }

        private long GetRecloserForHeadBreaker(long headBreakerId, OutageTopologyModel topology)
        {
            long recolserId = -1;

            if (!topology.OutageTopology.ContainsKey(headBreakerId))
            {
                string message = $"Head switch with gid: {headBreakerId} is not in a topology model.";
                Logger.LogError(message);
                throw new Exception(message);
            }
            long currentBreakerId = headBreakerId;
            while (currentBreakerId != 0)
            {
                //currentBreakerId = TopologyModel.OutageTopology[currentBreakerId].SecondEnd.Where(element => modelResourcesDesc.GetModelCodeFromId(element) == ModelCode.BREAKER).FirstOrDefault();
                currentBreakerId = lifecycleHelper.GetNextBreaker(currentBreakerId, topology);
                if (currentBreakerId == 0)
                {
                    continue;
                }

                if (!topology.OutageTopology.ContainsKey(currentBreakerId))
                {
                    string message = $"Switch with gid: 0X{currentBreakerId:X16} is not in a topology model.";
                    Logger.LogError(message);
                    throw new Exception(message);
                }

                if (!topology.OutageTopology[currentBreakerId].NoReclosing)
                {
                    recolserId = currentBreakerId;
                    break;
                }
            }

            return recolserId;
        }
        #endregion Private Methods
    }
}
