﻿using System.Runtime.Serialization;

namespace Outage.Common.UI
{
    [DataContract]
    public class UIMeasurement
    {
        [DataMember]
        public long Gid { get; set; }
        [DataMember]
        public float Value { get; set; }
        [DataMember]
        public string Type { get; set; }
    }
}
