using Autofac;
using Autofac.Core;
using AzureServiceBusExample.Autofac;
using AzureServiceBusExample.Bus;
using AzureServiceBusExample.Bus.Clients;
using AzureServiceBusExample.Bus.Messages;
using AzureServiceBusExample.Bus.Messages.OrderRequests;
using AzureServiceBusExample.Bus.Messages.ShippedOrders;
using AzureServiceBusExample.Processing;
using AzureServiceBusExample.Processing.Handlers;
using AzureServiceBusExample.Storages;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AzureServiceBusExample
{
    public class Program
    {
        static Program()
        {
            _container = new AutofacBuilder("global").Build();
            _killAllTokenSource = _container.Resolve<CancellationTokenSource>();
            _startup = new Startup(_container);
        }

        private static readonly IContainer _container;
        private static readonly CancellationTokenSource _killAllTokenSource;
        private static readonly Startup _startup;

        public static void Main()
        {
            Console.CancelKeyPress += Console_CancelKeyPress;

            _startup.CreateRegisteredServiceBusObjects(recreateObjects: false);
            
            Task.WaitAll
            (
                _startup.RunProcessors(),
                _startup.PollMarketplaces().ContinueWith(e => _killAllTokenSource.Cancel()),
                _startup.ShipOrders()
            );

            Console.WriteLine("All processors closed");
        }
       
        private static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            _killAllTokenSource.Cancel();
            e.Cancel = true;
        }
    }
}
