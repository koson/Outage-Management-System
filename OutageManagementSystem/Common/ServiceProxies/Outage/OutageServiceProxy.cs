﻿using Outage.Common.ServiceContracts.OMS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Outage.Common.ServiceProxies.Outage
{
    public class OutageServiceProxy : ClientBase<IOutageContract>, IOutageContract
    {
        public OutageServiceProxy(string endpointName)
            : base(endpointName)
        {
        }

        public List<OutageData> GetActiveOutages()
        {
            List<OutageData> outageModels = null;
            try
            {
                outageModels = Channel.GetActiveOutages();
            }
            catch(Exception e)
            {
                string message = "Exception in GetActiveOutages() proxy method.";
                LoggerWrapper.Instance.LogError(message, e);
                throw e;
            }

            return outageModels;
        }

        public List<OutageData> GetArchivedOutages()
        {
            List<OutageData> outageModels = null;
            try
            {
                outageModels = Channel.GetArchivedOutages();
            }
            catch (Exception e)
            {
                string message = "Exception in GetArchivedOutages() proxy method.";
                LoggerWrapper.Instance.LogError(message, e);
                throw e;
            }

            return outageModels;
        }

        public bool ReportOutage(long elementGid)
        {
            bool success;

            try
            {
                success = Channel.ReportOutage(elementGid);
            }
            catch (Exception e)
            {
                string message = "Exception in ReportOutage() proxy method.";
                LoggerWrapper.Instance.LogError(message, e);
                throw e;
            }

            return success;
        }
    }
}