﻿using Outage.Common;
using SCADA_Service.DistributedTransaction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace SCADA_Service
{
    public class SCADAService : IDisposable
    {

        private ILogger logger = LoggerWrapper.Instance;

        private List<ServiceHost> hosts = null;

        public SCADAService()
        {
            InitializeHosts();
        }

        public void Start()
        {
            StartHosts();
        }

        public void Dispose()
        {
            CloseHosts();
            GC.SuppressFinalize(this);
        }

        private void InitializeHosts()
        {
            hosts = new List<ServiceHost>()
            {
                new ServiceHost(typeof(SCADATransactionActor)),
                new ServiceHost(typeof(SCADAModelUpdateNotification)),
            };
        }

        private void StartHosts()
        {
            if (hosts == null || hosts.Count == 0)
            {
                throw new Exception("SCADA Services can not be opend because they are not initialized.");
            }

            string message = string.Empty;
            foreach (ServiceHost host in hosts)
            {
                host.Open();

                message = string.Format("The WCF service {0} is ready.", host.Description.Name);
                Console.WriteLine(message);
                logger.LogInfo(message);

                message = "Endpoints:";
                Console.WriteLine(message);
                logger.LogInfo(message);

                foreach (Uri uri in host.BaseAddresses)
                {
                    Console.WriteLine(uri);
                    logger.LogInfo(uri.ToString());
                }

                Console.WriteLine("\n");
            }

            //message = string.Format("Connection string: {0}", Config.Instance.ConnectionString);
            //Console.WriteLine(message);
            //logger.LogInfo(message);

            message = string.Format("Trace level: {0}", CommonTrace.TraceLevel);
            Console.WriteLine(message);
            logger.LogInfo(message);


            message = "The SCADA Service is started.";
            Console.WriteLine("\n{0}", message);
            logger.LogInfo(message);
        }

        private void CloseHosts()
        {
            if (hosts == null || hosts.Count == 0)
            {
                throw new Exception("SCADA Services can not be closed because they are not initialized.");
            }

            foreach (ServiceHost host in hosts)
            {
                host.Close();
            }

            string message = "The SCADA Services are gracefully closed.";
            logger.LogInfo(message);
            Console.WriteLine("\n\n{0}", message);
        }
    }
}
