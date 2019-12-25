﻿using CECommon;
using CECommon.Interfaces;
using CECommon.Model;
using NetworkModelServiceFunctions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TopologyElementsFuntions;

namespace TopologyBuilder
{
    public class GraphBuilder : IGraphBuilder
    {
        private readonly TopologyElementFactory topologyElementFactory = new TopologyElementFactory();

        private List<Field> fields = new List<Field>();
        private HashSet<long> visited = new HashSet<long>();
        private Stack<TopologyElement> stack = new Stack<TopologyElement>();

        public Topology CreateGraphTopology(long firstElementGid)
        {
            Topology topology = new Topology();
            TopologyElement firstNode = topologyElementFactory.CreateTopologyElement(firstElementGid);
           
            stack.Push(firstNode);
           
            while (stack.Count > 0)
            {
                var currentNode = stack.Pop();
                if (!visited.Contains(currentNode.Id))
                {
                    visited.Add(currentNode.Id);
                }

                var connectedElements = CheckIgnorable(currentNode.Id);
                foreach (var element in connectedElements)
                {
                    var newNode = ConnectTwoNodes(element, currentNode);          
                    //currentNode.SecondEnd.Add(newNode);
                    topology.AddRelation(currentNode.Id, newNode.Id);
                    stack.Push(newNode);
                }
                topology.AddNode(currentNode);
            }

            topology.FirstNode = firstNode;
            return topology;
        }
        private List<long> CheckIgnorable(long gid)
        {
            var list = GDAModelHelper.Instance.GetAllReferencedElements(gid).Where(e => !visited.Contains(e)).ToList();
            List<long> elements = new List<long>();
            foreach (var element in list)
            {
                if (TopologyHelper.Instance.GetElementTopologyStatus(element) == TopologyStatus.Ignorable)
                {
                    visited.Add(element);
                    elements.AddRange(CheckIgnorable(element));
                }
                else
                {
                    elements.Add(element);
                }
            }
            return elements;
        }
        private TopologyElement ConnectTwoNodes(long newElementGid, TopologyElement parent)
        {
            bool newElementIsField = TopologyHelper.Instance.GetElementTopologyStatus(newElementGid) == TopologyStatus.Field;
            bool parentElementIsField = TopologyHelper.Instance.GetElementTopologyStatus(parent.Id) == TopologyStatus.Field;

            TopologyElement newNode = topologyElementFactory.CreateTopologyElement(newElementGid);

            if (newElementIsField && !parentElementIsField)
            {
                var field = new Field(newNode);
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
                    throw new Exception($"Element with GID {parent.Id.ToString("X")} has no field.");
                }
               
            }
            else if (!newElementIsField && parentElementIsField)
            {
                var field = GetField(parent.Id);
                if (field == null)
                {
                    throw new Exception($"Element with GID {parent.Id.ToString("X")} has no field.");
                }
                else
                {
                    field.SecondEnd.Add(newNode);
                    newNode.FirstEnd = field;
                }
            }
            else
            {
                newNode.FirstEnd = parent;
                parent.SecondEnd.Add(newNode);
            }
            return newNode;
        }
        private Field GetField(long memberGid)
        {
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
    }
}