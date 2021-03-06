﻿using Outage.Common;
using Outage.SCADA.SCADAData.Repository;
using Outage.SCADA.SCADAService.Command;
using Outage.SCADA.SCADAService.DistributedTransaction;
using System;
using System.Text;
using System.Collections.Generic;
using System.ServiceModel;
using Outage.SCADA.SCADAService.IntegrityUpdate;
using Outage.SCADA.ModbusFunctions;
using Outage.SCADA.Modbus;

namespace Outage.SCADA.SCADAService
{
    public class SCADAService : IDisposable
    {
        private ILogger logger;

        protected ILogger Logger
        {
            get { return logger ?? (logger = LoggerWrapper.Instance); }
        }

        private readonly ModelResourcesDesc modelResourcesDesc;
        private readonly EnumDescs enumDescs;
        private readonly SCADAModel scadaModel;

        private FunctionExecutor functionExecutor;
        private Acquisition acquisition;

        private List<ServiceHost> hosts;

        public SCADAService()
        {
            modelResourcesDesc = new ModelResourcesDesc();
            enumDescs = new EnumDescs();
            scadaModel = new SCADAModel(modelResourcesDesc, enumDescs);
            functionExecutor = new FunctionExecutor(scadaModel);

            FunctionFactory.SCADAModel = scadaModel;
            SCADAModelUpdateNotification.SCADAModel = scadaModel;
            SCADATransactionActor.SCADAModel = scadaModel;
            CommandService.SCADAModel = scadaModel;
            IntegrityUpdateService.SCADAModel = scadaModel;

            CommandService.WriteCommandEnqueuer = functionExecutor;

            scadaModel.ImportModel();

            InitializeHosts();
        }

        #region Public Members
        public void Start()
        {
            try
            {
                ModbusSimulatorHandler.StartModbusSimulator();
                functionExecutor.StartExecutorThread();
                StartDataAcquisition();
                StartHosts();
            }
            catch (Exception e)
            {
                Logger.LogError("Exception in Start()", e);
                Console.WriteLine(e.Message);
            }
        }

        public void Dispose()
        {
            ModbusSimulatorHandler.StopModbusSimulaotrs();
            CloseHosts();
            
            if (acquisition != null)
            {
                acquisition.StopAcquisitionThread();
            }

            if(functionExecutor != null)
            {
                functionExecutor.StopExecutorThread();
            }

            GC.SuppressFinalize(this);
        }
        #endregion


        #region Private Members
        private void InitializeHosts()
        {
            hosts = new List<ServiceHost>()
            {
                new ServiceHost(typeof(IntegrityUpdateService)),
                new ServiceHost(typeof(CommandService)),
                new ServiceHost(typeof(SCADATransactionActor)),
                new ServiceHost(typeof(SCADAModelUpdateNotification))
            };
        }

        private void StartDataAcquisition()
        {
            acquisition = new Acquisition(functionExecutor, scadaModel);
            acquisition.StartAcquisitionThread();
        }

        private void StartHosts()
        {
            if (hosts == null || hosts.Count == 0)
            {
                throw new Exception("SCADA Service hosts can not be opend because they are not initialized.");
            }

            string message;
            StringBuilder sb = new StringBuilder();

            foreach (ServiceHost host in hosts)
            {
                host.Open();

                message = string.Format("The WCF service {0} is ready.", host.Description.Name);
                Console.WriteLine(message);
                sb.AppendLine(message);

                message = "Endpoints:";
                Console.WriteLine(message);
                sb.AppendLine(message);

                foreach (Uri uri in host.BaseAddresses)
                {
                    Console.WriteLine(uri);
                    sb.AppendLine(uri.ToString());
                }

                Console.WriteLine("\n");
                sb.AppendLine();
            }

            Logger.LogInfo(sb.ToString());

            message = "Trace level: LEVEL NOT SPECIFIED!";
            Console.WriteLine(message);
            Logger.LogWarn(message);

            message = "The SCADA Service is started.";
            Console.WriteLine("\n{0}", message);
            Logger.LogInfo(message);
        }

        private void CloseHosts()
        {
            if (hosts == null || hosts.Count == 0)
            {
                throw new Exception("SCADA Service hosts can not be closed because they are not initialized.");
            }

            foreach (ServiceHost host in hosts)
            {
                host.Close();
            }

            string message = "SCADA Service is gracefully closed.";
            Logger.LogInfo(message);
            Console.WriteLine("\n\n{0}", message);
        }
        #endregion
    }
}