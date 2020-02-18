﻿using CECommon.Providers;
using Outage.Common;
using Outage.Common.ServiceContracts.CalculationEngine;
using Outage.Common.ServiceContracts.SCADA;
using Outage.Common.ServiceProxies;
using System;

//TODO: namspace rename
namespace SCADACommanding
{
    public class SCADACommandingService : ISwitchStatusCommandingContract
    {
        private ILogger logger = LoggerWrapper.Instance;
        public bool SendAnalogCommand(long gid, float commandingValue)
        {
            bool success = false;
            //Imamo li analog komandu ????
            return success;
        }

        public void SendCommand(long guid, int value)
        {
            bool success = false;
            try
            {
                if (Provider.Instance.TopologyProvider.IsElementRemote(Provider.Instance.CacheProvider.GetElementGidForMeasurement(guid)))
                {
                    ProxyFactory proxyFactory = new ProxyFactory();

                    using (SCADACommandProxy proxy = proxyFactory.CreateProxy<SCADACommandProxy, ISCADACommand>(EndpointNames.SCADACommandService))
                    {
                        if (proxy == null)
                        {
                            string message = "SendDiscreteCommand => SCADACommandProxy is null.";
                            logger.LogError(message);
                            throw new NullReferenceException(message);
                        }

                        success = proxy.SendDiscreteCommand(guid, (ushort)value);
                    }
                }
                else
                {
                    //todo: sucess = what?
                    Provider.Instance.CacheProvider.UpdateDiscreteMeasurement(guid, (ushort)value);
                }
            }
            catch (Exception ex)
            {
                success = false;
                logger.LogError($"Sending discrete command for measurement with GID {guid} failed. Exception: {ex.Message}");
            }

            //return success;
        }
    }
}
