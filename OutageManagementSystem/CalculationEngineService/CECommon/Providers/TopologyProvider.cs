﻿using CECommon.Interfaces;
using Outage.Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CECommon.Providers
{
    public class TopologyProvider : ITopologyProvider
    {
        private ILogger logger =  LoggerWrapper.Instance;
        private TransactionFlag transactionFlag;
        private IVoltageFlow voltageFlow;
        private List<ITopology> topology;
        private IModelTopologyService modelTopologyServis;
        private HashSet<long> reclosers;
        private List<ITopology> Topology
        {
            get { return topology; }
            set
            {
                topology = value;
                ProviderTopologyDelegate?.Invoke(Topology);
            }
        }
        private List<ITopology> TransactionTopology { get; set; }
        public ProviderTopologyDelegate ProviderTopologyDelegate { get; set; }
        public ProviderTopologyConnectionDelegate ProviderTopologyConnectionDelegate{get; set;}
        public TopologyProvider(IModelTopologyService modelTopologyServis, IVoltageFlow voltageFlow)
        {
            this.voltageFlow = voltageFlow;
            this.modelTopologyServis = modelTopologyServis;
            transactionFlag = TransactionFlag.NoTransaction;
            Topology = this.modelTopologyServis.CreateTopology();
            reclosers = Provider.Instance.ModelProvider.GetReclosers();
            Provider.Instance.MeasurementProvider.DiscreteMeasurementDelegate += DiscreteMeasurementDelegate;
            Provider.Instance.TopologyProvider = this;
        }

        public void DiscreteMeasurementDelegate(List<long> elementGids)
        {
            voltageFlow.UpdateLoadFlow(Topology);
            ProviderTopologyDelegate?.Invoke(Topology);
        }

        public List<ITopology> GetTopologies()
        {
            if (transactionFlag == TransactionFlag.NoTransaction)
            {
                return Topology;
            }
            else
            {
                return TransactionTopology;
            }
        }
        public void CommitTransaction()
        {
            Topology = TransactionTopology;
            transactionFlag = TransactionFlag.NoTransaction;
            reclosers = Provider.Instance.ModelProvider.GetReclosers();
            ProviderTopologyConnectionDelegate?.Invoke(Topology);
        }
        public bool PrepareForTransaction()
        {
            bool success = true;
            try
            {
                logger.LogInfo($"Model provider preparing for transaction.");
                TransactionTopology = modelTopologyServis.CreateTopology();
                transactionFlag = TransactionFlag.InTransaction;
            }
            catch (Exception ex)
            {
                logger.LogInfo($"Model provider failed to prepare for transaction. Exception message: {ex.Message}");
                success = false;
            }
            return success;
        }
        public void RollbackTransaction()
        {
            TransactionTopology = null;
            transactionFlag = TransactionFlag.NoTransaction;
        }
        public bool IsElementRemote(long elementGid)
        {
            bool isRemote = false;
            foreach (var topology in Topology)
            {
                if (topology.TopologyElements.TryGetValue(elementGid, out ITopologyElement element))
                {
                    isRemote = element.IsRemote;
                    break;
                }
            }
            return isRemote;
        }
    }
}
