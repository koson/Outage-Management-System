﻿using CECommon;
using CECommon.Interfaces;
using CECommon.Model;
using CECommon.Models;
using Common.CeContracts;
using OMS.Common.Cloud;
using OMS.Common.Cloud.Logger;
using OMS.Common.NmsContracts;
using OMS.Common.WcfClient.CE;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TopologyBuilderImplementation
{
    public class TopologyBuilder : ITopologyBuilderContract
    {
        #region Fields
        private List<Field> fields;
        private HashSet<long> visited;
        private HashSet<long> reclosers;
        private Stack<long> stack;
        private Dictionary<long, ITopologyElement> elements;
        private Dictionary<long, List<long>> connections;
        #endregion

        private readonly string baseLogString;

        private readonly MeasurementProviderClient measurementProviderClient;
        private readonly ModelProviderClient modelProviderClient;

        private ICloudLogger logger;
        private ICloudLogger Logger
        {
            get { return logger ?? (logger = CloudLoggerFactory.GetLogger()); }
        }

        public TopologyBuilder()
        {
            this.baseLogString = $"{this.GetType()} [{this.GetHashCode()}] =>{Environment.NewLine}";
            string verboseMessage = $"{baseLogString} entering Ctor.";
            Logger.LogVerbose(verboseMessage);

            measurementProviderClient = MeasurementProviderClient.CreateClient();
            modelProviderClient = ModelProviderClient.CreateClient();

            string debugMessage = $"{baseLogString} Ctor => Clients initialized.";
            Logger.LogDebug(debugMessage);
        }

        public async Task<ITopology> CreateGraphTopology(long firstElementGid)
        {
            Logger.LogVerbose($"{baseLogString} CreateGraphTopology method called.");

            ITopologyElement currentFider = null;

            Logger.LogDebug($"{baseLogString} CreateGraphTopology => Calling GetElementModels method from model provider.");
            elements = await modelProviderClient.GetElementModels();
            Logger.LogDebug($"{baseLogString} CreateGraphTopology => GetElementModels method from model provider has been called successfully.");

            Logger.LogDebug($"{baseLogString} CreateGraphTopology => Calling GetConnections method from model provider.");
            connections = await modelProviderClient.GetConnections();
            Logger.LogDebug($"{baseLogString} CreateGraphTopology => GetConnections method from model provider has been called successfully.");

            Logger.LogDebug($"{baseLogString} CreateGraphTopology => Calling GetReclosers method from model provider.");
            reclosers = await modelProviderClient.GetReclosers();
            Logger.LogDebug($"{baseLogString} CreateGraphTopology => GetReclosers method from model provider has been called successfully.");

            Logger.LogDebug($"{baseLogString} CreateGraphTopology => Creating topology from first element with GID {firstElementGid:X16} started.");

            visited = new HashSet<long>();
            stack = new Stack<long>();
            fields = new List<Field>();

            ITopology topology = new TopologyModel
            {
                FirstNode = firstElementGid
            };

            stack.Push(firstElementGid);
            ITopologyElement currentElement;
            long currentElementId = 0;

            while (stack.Count > 0)
            {
                currentElementId = stack.Pop();
                if (!visited.Contains(currentElementId))
                {
                    visited.Add(currentElementId);
                }

                if (!elements.TryGetValue(currentElementId, out currentElement))
                {
                    string message = $"{baseLogString} CreateGraphTopology => Failed to build topology.Topology does not contain element with GID {currentElementId:X16}.";
                    Logger.LogError(message);
                    return topology;
                    //throw new Exception(message);
                }

                foreach (var element in GetReferencedElementsWithoutIgnorables(currentElementId))
                {
                    if (elements.TryGetValue(element, out ITopologyElement newNode))
                    {
                        if (!reclosers.Contains(element))
                        {
                            ConnectTwoNodes(newNode, currentElement);
                            stack.Push(element);
                        }
                        else
                        {
                            currentElement.SecondEnd.Add(newNode);
                            if (newNode.FirstEnd == null)
                            {
                                newNode.FirstEnd = currentElement;
                            }
                            else
                            {
                                newNode.SecondEnd.Add(currentElement);
                            }

                            if (!topology.TopologyElements.ContainsKey(newNode.Id))
                            {
                                topology.AddElement(newNode);
                            }
                        }
                    }
                    else
                    {
                        Logger.LogError($"{baseLogString} CreateGraphTopology => Element with GID {element:X16} does not exist in collection of elements.");
                    }
                }

                if (currentElement is Feeder)
                {
                    currentFider = currentElement;
                }
                else
                {
                    currentElement.Feeder = currentFider;
                }

                topology.AddElement(currentElement);
            }

            foreach (var field in fields)
            {
                topology.AddElement(field);
            }

            Logger.LogDebug($"{baseLogString} CreateGraphTopology => Topology successfully created.");
            return topology;
        }

        #region HelperFunctions
        private List<long> GetReferencedElementsWithoutIgnorables(long gid)
        {
            string verboseMessage = $"{baseLogString} GetReferencedElementsWithoutIgnorables method called for GID {gid:X16}.";
            Logger.LogVerbose(verboseMessage);

            List<long> refElements = new List<long>();

            if (connections.TryGetValue(gid, out List<long> list))
            {
                list = list.Where(e => !visited.Contains(e)).ToList();

                foreach (var element in list)
                {
                    DMSType elementType = (DMSType)ModelCodeHelper.ExtractTypeFromGlobalId(element);

                    if (TopologyHelper.Instance.GetElementTopologyStatus(element) == TopologyStatus.Ignorable)
                    {
                        visited.Add(element);
                        refElements.AddRange(GetReferencedElementsWithoutIgnorables(element));
                    }
                    else if (elementType != DMSType.DISCRETE && elementType != DMSType.ANALOG && elementType != DMSType.BASEVOLTAGE)
                    {
                        refElements.Add(element);
                    }
                }
            }
            else
            {
                Logger.LogWarning($"{baseLogString} GetReferencedElementsWithoutIgnorables => Failed to get connected elements for element with GID {gid:X16)}.");
            }

            return refElements;
        }
        private void ConnectTwoNodes(ITopologyElement newNode, ITopologyElement parent)
        {
            string verboseMessage = $"{baseLogString} ConnectTwoNodes method called. New node GID {newNode?.Id:X16}, Parent node GID {parent?.Id:X16}.";
            Logger.LogVerbose(verboseMessage);

            if (newNode == null)
            {
                string message = $"{baseLogString} ConnectTwoNodes => New node is null.";
                Logger.LogError(message);
                throw new Exception(message);
            }

            if (parent == null)
            {
                string message = $"{baseLogString} ConnectTwoNodes => Parent node is null.";
                Logger.LogError(message);
                throw new Exception(message);
            }

            bool newElementIsField = TopologyHelper.Instance.GetElementTopologyStatus(newNode.Id) == TopologyStatus.Field;
            bool parentElementIsField = TopologyHelper.Instance.GetElementTopologyStatus(parent.Id) == TopologyStatus.Field;

            if (newElementIsField && !parentElementIsField)
            {
                var field = new Field(newNode);
                field.FirstEnd = parent;
                newNode.FirstEnd = parent;
                fields.Add(field);
                parent.SecondEnd.Add(field);
            }
            else if (newElementIsField && parentElementIsField)
            {
                try
                {
                    GetField(parent.Id).Members.Add(newNode);
                    newNode.FirstEnd = parent;
                    parent.SecondEnd.Add(newNode);
                }
                catch (Exception)
                {
                    string message = $"{baseLogString} ConnectTwoNodes => Element with GID {parent.Id:X16} has no field.";
                    Logger.LogError(message);
                    throw new Exception(message);
                }

            }
            else if (!newElementIsField && parentElementIsField)
            {
                var field = GetField(parent.Id);
                if (field == null)
                {
                    string message = $"{baseLogString} ConnectTwoNodes => Element with GID {parent.Id:X16} has no field.";
                    Logger.LogError(message);
                    throw new Exception(message);
                }
                else
                {
                    field.SecondEnd.Add(newNode);
                    parent.SecondEnd.Add(newNode);
                    newNode.FirstEnd = field;
                }
            }
            else
            {
                newNode.FirstEnd = parent;
                parent.SecondEnd.Add(newNode);
            }
        }
        private Field GetField(long memberGid)
        {
            string verboseMessage = $"{baseLogString} GetField method called. Member GID {memberGid:X16}";
            Logger.LogVerbose(verboseMessage);

            Field field = null;
            for (int i = 0; i < fields.Count; i++)
            {
                if (fields[i].Members.Where(e => e.Id == memberGid).ToList().Count > 0)
                {
                    return fields[i];
                }
            }
            return field;
        }
        #endregion
    }
}
