﻿using OMS.OutageSimulator.UserControls;
using Outage.Common.ServiceContracts.OMS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OMS.OutageSimulator.Services
{
    public class OutageSimulatorService : IOutageSimulatorContract
    {
        public static Overview Overview { get; set; }

        public bool StopOutageSimulation(long outageElementId)
        {
            return Overview.StopOutageSimulation(outageElementId);
        }

        public bool IsOutageElement(long outageElementId)
        {
            var retVal = Overview.ActiveOutages.Where(outage => outage.OutageElement.GID == outageElementId).ToList();
            return retVal.Count > 0;
        }
    }
}
