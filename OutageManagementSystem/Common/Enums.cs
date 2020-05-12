﻿using System.Runtime.Serialization;

namespace Outage.Common
{
    //NMS
    public enum AnalogMeasurementType : short
    {
        VOLTAGE = 1,
        CURRENT = 2,
        FEEDER_CURRENT = 3,
        POWER   = 4,
    }

    public enum DiscreteMeasurementType : short
    {
        SWITCH_STATUS   = 1,
    }


    //PUB_SUB
    [DataContract]
    public enum Topic
    {
        [EnumMember]
        MEASUREMENT = 1,

        [EnumMember]
        SWITCH_STATUS,

        [EnumMember]
        TOPOLOGY,

        [EnumMember]
        OUTAGE_EMAIL,

        [EnumMember]
        ACTIVE_OUTAGE,

        [EnumMember]
        ARCHIVED_OUTAGE,

        [EnumMember]
        OMS_MODEL
    }

    //SCADA
    [DataContract]
    public enum AlarmType : short
    {
        [EnumMember]
        NO_ALARM = 0x01,

        [EnumMember]
        REASONABILITY_FAILURE = 0x02,

        [EnumMember]
        LOW_ALARM = 0x03,

        [EnumMember]
        HIGH_ALARM = 0x04,

        [EnumMember]
        ABNORMAL_VALUE = 0x05,
    }

    [DataContract]
    public enum DiscreteCommandingType : ushort
    {
        [EnumMember]
        CLOSE = 0x00,
        
        [EnumMember]
        OPEN = 0x01,
    }

    [DataContract]
    public enum ElementType
    {
        
        [EnumMember]
        Remote = 1,

        [EnumMember]
        Local
    }
    
    [DataContract]
    public enum CommandOriginType : short
    {
        [EnumMember]
        USER_COMMAND = 0x1,

        [EnumMember]
        ISOLATING_ALGORITHM_COMMAND,

        [EnumMember]
        CE_COMMAND,

        [EnumMember]
        MODEL_UPDATE_COMMAND,

        [EnumMember]
        OUTAGE_SIMULATOR,

        [EnumMember]
        NON_SCADA_OUTAGE,

        [EnumMember]
        OTHER_COMMAND, //TODO: rethink of name, add others like CE ili tako nesto
    }

    //OMS
    [DataContract]
    public enum OutageState : short
    {
        [EnumMember]
        CREATED = 1,
        [EnumMember]
        ISOLATED = 2,
        [EnumMember]
        REPAIRED = 3,
        [EnumMember]
        ARCHIVED = 4,
    }

    [DataContract]
    public enum DatabaseOperation : short
    {
        [EnumMember]
        INSERT = 1,
        [EnumMember]
        DELETE = 2
    }
}
