﻿using OMS.Web.UI.Models.ViewModels;
using Outage.Common.UI;
using System.Collections.Generic;

namespace OMS.Web.Common.Mappers
{
    public class GraphMapper : IGraphMapper
    {
        public OmsGraph MapTopology(UIModel topologyModel)
        {
            OmsGraph graph = new OmsGraph();

            // map nodes
            foreach (KeyValuePair<long, UINode> keyValue in topologyModel.Nodes)
            {
                Node graphNode = new Node
                {
                    Id = keyValue.Value.Gid.ToString(),
                    IsActive = keyValue.Value.IsActive,
                    Type = keyValue.Value.Type,
                    Value = keyValue.Value.Measurement
                };

                graph.Nodes.Add(graphNode);
            }

            // map relations
            foreach (KeyValuePair<long, HashSet<long>> keyValue in topologyModel.Relations) 
            {
                foreach(long targetNodeId in keyValue.Value)
                {
                    Relation graphRelation = new Relation
                    {
                        SourceNodeId = keyValue.Key.ToString(),
                        TargetNodeId = targetNodeId.ToString()
                    };

                    graph.Relations.Add(graphRelation);
                }
            }

            return graph;
        }
    }
}