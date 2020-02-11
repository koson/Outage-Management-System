﻿using Outage.Common.OutageService.Interface;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Outage.Common.OutageService.Model
{
    [DataContract]
    public class OutageTopologyElement : IOutageTopologyElement
    {
        #region Fields
        private long id;
        private long firstEnd;
        private List<long> secondEnd;
        private string dmsType;
        private bool isRemote;
        private ushort distanceFromSource;
        #endregion


        #region Properties
        [DataMember]
        public long Id { get { return id; } set { id = value; } }
        [DataMember]
        public long FirstEnd { get { return firstEnd; } set { firstEnd = value; } }
        [DataMember]
        public List<long> SecondEnd { get { return secondEnd; } set { secondEnd = value; } }
        [DataMember]
        public string DmsType { get { return dmsType; } set { dmsType = value; } }
        [DataMember]
        public bool IsRemote { get { return isRemote; } set { isRemote = value; } }
        [DataMember]
        public ushort DistanceFromSource { get { return distanceFromSource; } set { distanceFromSource = value; } }
        #endregion

        public OutageTopologyElement(long gid)
        {
            this.Id = gid;
            this.SecondEnd = new List<long>();
        }
    }
}