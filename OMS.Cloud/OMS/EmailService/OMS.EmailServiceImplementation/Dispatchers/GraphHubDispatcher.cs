﻿using Microsoft.AspNet.SignalR.Client;
using OMS.EmailImplementation.Interfaces;
using OMS.Common.Cloud.Logger;
using System;
using System.Configuration;

namespace OMS.EmailImplementation.Dispatchers
{
    public class GraphHubDispatcher : IDispatcher
    {
        private ICloudLogger logger;
        private ICloudLogger Logger
        {
            get { return logger ?? (logger = CloudLoggerFactory.GetLogger()); }
        }

        private readonly string url;
        private readonly string hubName;

        private readonly HubConnection connection;
        private readonly IHubProxy proxy;

        public bool IsConnected { get; private set; }

        public GraphHubDispatcher()
        {
            url = ConfigurationManager.AppSettings["hubUrl"];
            hubName = ConfigurationManager.AppSettings["hubName"];
            IsConnected = false;

            connection = new HubConnection(url);
            proxy = connection.CreateHubProxy(hubName);
        }

        public void Connect()
        {
            connection.Start().ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    Console.WriteLine($"[GraphHubDispatcher::Connect] Could not connect to Graph SignalR Hub.");
                }
                else
                {
                    Console.WriteLine($"[GraphHubDispatcher::Connect] Connected to {hubName}. ");
                    IsConnected = true;
                }
            }).Wait();
        }

        public void Dispatch(long gid)
        {
            if (!IsConnected)
			{
                Connect();
			}

            try
            {
                Logger.LogInformation($"[GraphHubDispatcher::Dispatch] Sending graph outage call update to Graph Hub");
                //Console.WriteLine($"[GraphHubDispatcher::Dispatch] Sending graph outage call update to Graph Hub");
                proxy.Invoke<string>("NotifyGraphOutageCall", gid)?.Wait();
            }
            catch (Exception)
            {
                Logger.LogError($"[GraphHubDispatcher::Dispatch] Sending graph outage call update failed.");
                //Console.WriteLine($"[GraphHubDispatcher::Dispatch] Sending graph outage call update failed.");
            }
        }

        public void Stop()
        {
            if (IsConnected)
			{
                connection.Stop();
			}

            IsConnected = false;
        }

        ~GraphHubDispatcher()
        {
            Stop();
        }
    }
}
