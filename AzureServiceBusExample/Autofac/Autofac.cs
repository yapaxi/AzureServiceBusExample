﻿using Autofac;
using Autofac.Core;
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
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AzureServiceBusExample.Autofac
{
    public class AutofacBuilder
    {
        private readonly ContainerBuilder _builder;
        private readonly EnvironmentNamespaceManager _envNS;
        private readonly TimeSpan? _cloudEntityAutoDeleteOnIdle;

        public AutofacBuilder(string busConnectionString, string rootNamespace, TimeSpan? cloudEntityAutoDeleteOnIdle = null)
        {
            _builder = new ContainerBuilder();
            _cloudEntityAutoDeleteOnIdle = cloudEntityAutoDeleteOnIdle;

            _builder.Register(e => MessagingFactory.CreateFromConnectionString(busConnectionString)).SingleInstance();
            _builder.RegisterInstance(_envNS = new EnvironmentNamespaceManager(rootNamespace));
            _builder.RegisterInstance(NamespaceManager.CreateFromConnectionString(busConnectionString));
        }

        public IContainer Build()
        {
            // input queues for marketplace order requests
            RegisterQueue<AmazonOrderRequest>();
            RegisterQueue<JetOrderRequest>();
            RegisterQueue<R1OrderRequest>();

            _builder.RegisterType<AmazonOrderRequestHandler>().As<IMessageHandler<AmazonOrderRequest, R1OrderRequest>>();
            _builder.RegisterType<JetOrderRequestHandler>().As<IMessageHandler<JetOrderRequest, R1OrderRequest>>();

            // mm -> r1
            _builder.RegisterType<InputOutputMessageQueueProcessor<AmazonOrderRequest, R1OrderRequest>>().As<IMessageProcessor>();
            _builder.RegisterType<InputOutputMessageQueueProcessor<JetOrderRequest, R1OrderRequest>>().As<IMessageProcessor>();

            // r1 -> *
            _builder.RegisterType<R1OrderRequestHandler>().As<IMessageHandler<R1OrderRequest>>();
            _builder.RegisterType<InputMessageQueueProcessor<R1OrderRequest>>().As<IMessageProcessor>();

            RegisterTopic<ShippedOrder>(e => e.MarketplaceName);

            // r1 -> marketplace
            _builder.RegisterType<AmazonShippedOrderHandler>();
            _builder.RegisterType<JetShippedOrderHandler>();

            SubscribeOnTopic<AmazonShippedOrderHandler, ShippedOrder>(e => e.MarketplaceName, AmazonOrderRequestHandler.MarketplaceName);
            SubscribeOnTopic<JetShippedOrderHandler, ShippedOrder>(e => e.MarketplaceName, JetOrderRequestHandler.MarketplaceName);

            // other
            _builder.RegisterInstance(new Storage()).As<IStorage>();
            _builder.RegisterInstance(new CancellationTokenSource());

            return _builder.Build();
        }

        private void RegisterQueue<TMessage>()
        {
            _builder.RegisterInstance(new QueueDescription(_envNS.ResolvePath<TMessage>())
            {
                AutoDeleteOnIdle = _cloudEntityAutoDeleteOnIdle ?? TimeSpan.Zero
            });

            _builder.RegisterType<QueueMessageClient<TMessage>>()
                .SingleInstance()
                .As<IMessageDestination<TMessage>>()
                .As<IMessageSource<TMessage>>()
                .AsSelf();
        }

        private void RegisterTopic<TMessage>(Expression<Func<TMessage, string>> selector)
            where TMessage : ITopicFilteredMessage
        {
            _builder.RegisterInstance(new TopicDescription(_envNS.ResolvePath<TMessage>())
            {
                AutoDeleteOnIdle = _cloudEntityAutoDeleteOnIdle ?? TimeSpan.Zero
            });

            var filterPropertyName = GetSelectorPropertyName(selector);

            _builder.RegisterType<TopicMessageClient<TMessage>>()
                .WithParameter(new TypedParameter(typeof(string), filterPropertyName))
                .SingleInstance()
                .As<IMessageDestination<TMessage>>()
                .AsSelf();
        }

        private void SubscribeOnTopic<TSubscriptionsHandler, TTopicMessage>(Expression<Func<TTopicMessage, string>> selector, string filterValue)
            where TSubscriptionsHandler : IMessageHandler<TTopicMessage>
        {
            _builder.RegisterInstance(new Tuple<SubscriptionDescription, SqlFilter>(
                new SubscriptionDescription(_envNS.ResolvePath<TTopicMessage>(), filterValue)
                {
                    AutoDeleteOnIdle = _cloudEntityAutoDeleteOnIdle ?? TimeSpan.Zero
                },
                new SqlFilter($"{GetSelectorPropertyName(selector)} = '{filterValue}'")));

            _builder
                .RegisterType<SubscriptionMessageClient<TTopicMessage>>()
                .WithParameter(new TypedParameter(typeof(string), filterValue))
                .Named<SubscriptionMessageClient<TTopicMessage>>(filterValue)
                .SingleInstance()
                .As<IMessageSource<TTopicMessage>>()
                .AsSelf();

            _builder.Register(e => new InputMessageSubscriptionProcessor<TTopicMessage, TSubscriptionsHandler>(
                handler: e.Resolve<TSubscriptionsHandler>(),
                subscription: e.ResolveNamed<SubscriptionMessageClient<TTopicMessage>>(filterValue),
                tokenSource: e.Resolve<CancellationTokenSource>()
            ))
            .SingleInstance()
            .As<IMessageProcessor>();
        }

        private static string GetSelectorPropertyName<TMessage>(Expression<Func<TMessage, string>> selector)
        {
            var memberExpression = (MemberExpression)selector.Body;
            var propertyName = memberExpression.Member.Name;
            return propertyName;
        }
    }
}
