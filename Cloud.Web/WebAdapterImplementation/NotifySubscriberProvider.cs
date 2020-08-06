﻿using Common.CeContracts;
using Common.PubSub;
using Common.PubSubContracts.DataContracts.OMS;
using Common.Web.Mappers;
using Common.Web.Models.ViewModels;
using OMS.Common.PubSubContracts;
using OMS.Common.PubSubContracts.DataContracts.SCADA;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WebAdapterImplementation.HubDispatchers;

namespace WebAdapterImplementation
{
    public class NotifySubscriberProvider : INotifySubscriberContract
    {
        private readonly string _subscriberName;
        private GraphHubDispatcher _graphDispatcher;
        private OutageHubDispatcher _outageDispatcher;
        private ScadaHubDispatcher _scadaDispatcher;
        private readonly IGraphMapper _graphMapper;
        private readonly IOutageMapper _outageMapper;

        public NotifySubscriberProvider(string subscriberName)
        {
            _subscriberName = subscriberName;
            _graphMapper = new GraphMapper();
            _outageMapper = new OutageMapper(new ConsumerMapper(), new EquipmentMapper());
        }

        public async Task Notify(IPublishableMessage message, string publisherName)
        {
            if (message is ActiveOutageMessage activeOutage)
            {
                _outageDispatcher = new OutageHubDispatcher(_outageMapper);
                _outageDispatcher.Connect();

                try
                {
                    _outageDispatcher.NotifyActiveOutageUpdate(activeOutage);
                }
                catch (Exception)
                {
                    // TODO: log error
                }
            }
            else if (message is ArchivedOutageMessage archivedOutage)
            {
                _outageDispatcher.Connect();

                try
                {
                    _outageDispatcher.NotifyArchiveOutageUpdate(archivedOutage);
                }
                catch (Exception)
                {
                    // TODO: log error
                }
            }
            else if (message is TopologyForUIMessage topologyMessage)
            {
                OmsGraphViewModel graph = _graphMapper.Map(topologyMessage.UIModel);

                _graphDispatcher.Connect();
                try
                {
                    _graphDispatcher.NotifyGraphUpdate(graph.Nodes, graph.Relations);
                }
                catch (Exception)
                {
                    // TODO: log error/retry
                }

            } else if (message is MultipleAnalogValueSCADAMessage analogValuesMessage)
            {
                Dictionary<long, AnalogModbusData> analogModbusData = new Dictionary<long, AnalogModbusData>(analogValuesMessage.Data);
                _scadaDispatcher = new ScadaHubDispatcher();
                _scadaDispatcher.Connect();

                try
                {
                    _scadaDispatcher.NotifyScadaDataUpdate(analogModbusData);
                }
                catch (Exception)
                {
                    // TODO: log error
                }
            }
        }

        public async Task<string> GetSubscriberName()
        {
            return _subscriberName;
        }
    }
}
