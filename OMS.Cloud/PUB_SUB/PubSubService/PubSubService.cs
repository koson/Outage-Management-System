﻿using System;
using System.Collections.Generic;
using System.Fabric;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Communication.Wcf;
using Microsoft.ServiceFabric.Services.Communication.Wcf.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using OMS.Common.Cloud.Logger;
using OMS.Common.Cloud.Names;
using OMS.Common.PubSub;
using OMS.Common.PubSubContracts;

using PubSubImplementation;

namespace PubSubService
{
    /// <summary>
    /// An instance of this class is created for each service replica by the Service Fabric runtime.
    /// </summary>
    internal sealed class PubSubService : StatefulService
    {
        private readonly ICloudLogger logger;

        private readonly PublisherProvider publisherProvider;
        private readonly RegisterSubscriberProvider registerSubscriberProvider;

        public PubSubService(StatefulServiceContext context)
            : base(context)
        {
            logger = CloudLoggerFactory.GetLogger();

            try
            {
                this.publisherProvider = new PublisherProvider(this.StateManager);
                this.registerSubscriberProvider = new RegisterSubscriberProvider(this.StateManager);

                string message = "Contract providers initialized.";
                logger.LogInformation(message);
                ServiceEventSource.Current.ServiceMessage(this.Context, $"[PubSubService | Information] {message}");
            }
            catch (Exception e)
            {
                logger.LogError(e.Message, e);
                ServiceEventSource.Current.ServiceMessage(this.Context, $"[PubSubService | Error] {e.Message}");
            }
        }

        /// <summary>
        /// Optional override to create listeners (e.g., HTTP, Service Remoting, WCF, etc.) for this service replica to handle client or user requests.
        /// </summary>
        /// <remarks>
        /// For more information on service communication, see https://aka.ms/servicefabricservicecommunication
        /// </remarks>
        /// <returns>A collection of listeners.</returns>
        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            //return new ServiceReplicaListener[0];
            return new[]
            {
                //PublisherEndpoint
                new ServiceReplicaListener(context =>
                {
                    return new WcfCommunicationListener<IPublisherContract>(context,
                                                                            this.publisherProvider,
                                                                            WcfUtility.CreateTcpListenerBinding(),
                                                                            EndpointNames.PublisherEndpoint);
                }, EndpointNames.PublisherEndpoint),

                //SubscriberEndpoint
                new ServiceReplicaListener(context =>
                {
                    return new WcfCommunicationListener<IRegisterSubscriberContract>(context,
                                                                                     this.registerSubscriberProvider,
                                                                                     WcfUtility.CreateTcpListenerBinding(),
                                                                                     EndpointNames.SubscriberEndpoint);
                }, EndpointNames.SubscriberEndpoint),
            };
        }

        /// <summary>
        /// This is the main entry point for your service replica.
        /// This method executes when this replica of your service becomes primary and has write status.
        /// </summary>
        /// <param name="cancellationToken">Canceled when Service Fabric needs to shut down this service replica.</param>
        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                InitializeReliableCollections();

                string message = "ReliableDictionaries initialized.";
                logger.LogInformation(message);
                ServiceEventSource.Current.ServiceMessage(this.Context, $"[PubSubService | Information] {message}");
            }
            catch (Exception e)
            {
                logger.LogError(e.Message, e);
                ServiceEventSource.Current.ServiceMessage(this.Context, $"[PubSubService | Error] {e.Message}");
            }
        }

        private void InitializeReliableCollections()
        {
            Task[] tasks = new Task[]
            {
                Task.Run(async() =>
                {
                    using (ITransaction tx = this.StateManager.CreateTransaction())
                    {
                        var result = await StateManager.TryGetAsync<IReliableDictionary<short, Dictionary<Uri, RegisteredSubscriber>>>(ReliableDictionaryNames.RegisteredSubscribersCache);
                        if(result.HasValue)
                        {
                            var subscribers = result.Value;
                            await subscribers.ClearAsync();
                            await tx.CommitAsync();
                        }
                        else
                        {
                            await StateManager.GetOrAddAsync<IReliableDictionary<short, Dictionary<Uri, RegisteredSubscriber>>>(tx, ReliableDictionaryNames.RegisteredSubscribersCache);
                            await tx.CommitAsync();
                        }
                    }
                }),
            };

            Task.WaitAll(tasks);
        }
    }
}
