using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Autofac;
using AzureServiceBusExample.Autofac;
using AzureServiceBusExample;
using AzureServiceBusExample.Bus.Messages.OrderRequests;
using AzureServiceBusExample.Bus.Clients;
using System.Threading.Tasks;
using Microsoft.ServiceBus.Messaging;
using AzureServiceBusExample.Bus;
using System.Transactions;

namespace Tests
{
    [TestClass]
    public class UnitTest1
    {
        private static IContainer _container;
        private static Startup _startup;
        private static QueueMessageClient<JetOrderRequest> _jetOrderRequests1;
        private static QueueMessageClient<JetOrderRequest> _jetOrderRequests2;

        [ClassInitialize]
        public static void Init(TestContext context)
        {
            _container = new AutofacBuilder(rootNamespace: typeof(UnitTest1).FullName).Build();
            _startup = new Startup(_container);
            _startup.CreateRegisteredServiceBusObjects(recreateObjects: true);
            _jetOrderRequests1 = CreateClient();
            _jetOrderRequests2 = CreateClient();
        }

        private static QueueMessageClient<JetOrderRequest> CreateClient()
        {
            return new QueueMessageClient<JetOrderRequest>(
                            _container.Resolve<MessagingFactory>(),
                            _container.Resolve<EnvironmentNamespaceManager>());
        }

        [TestMethod]
        public async Task COMPLETE_MESSAGE_NEVER_RETURNS()
        {
            var key = Guid.NewGuid().ToString();
            await _jetOrderRequests1.SendMesage(new JetOrderRequest() { JetVenueOrderId = key });

            var message = await _jetOrderRequests1.ReceiveMessageAsync();
            Assert.AreEqual(key, message.GetBody<JetOrderRequest>().JetVenueOrderId);
            await message.CompleteAsync();

            message = await _jetOrderRequests2.ReceiveMessageAsync(TimeSpan.FromSeconds(5));

            Assert.IsNull(message);
        }

        [TestMethod]
        public async Task DEFERED_MESSAGE_CAN_BE_TAKEN_AGAIN_WITH_SEQUENCE_NUMBER()
        {
            var key = Guid.NewGuid().ToString();
            await _jetOrderRequests1.SendMesage(new JetOrderRequest() { JetVenueOrderId = key });

            var message = await _jetOrderRequests1.ReceiveMessageAsync();
            Assert.AreEqual(key, message.GetBody<JetOrderRequest>().JetVenueOrderId);
            await message.DeferAsync();
            
            message = await _jetOrderRequests2.ReceiveMessage(message.SequenceNumber);
            Assert.AreEqual(key, message.GetBody<JetOrderRequest>().JetVenueOrderId);
            await message.CompleteAsync();
        }

        [TestMethod]
        public async Task COMPLETE_CLONE_AND_RESEND()
        {
            var key = Guid.NewGuid().ToString();
            await _jetOrderRequests1.SendMesage(new JetOrderRequest() { JetVenueOrderId = key });

            var message = await _jetOrderRequests1.ReceiveMessageAsync();
            Assert.AreEqual(key, message.GetBody<JetOrderRequest>().JetVenueOrderId);
            var clone = message.Clone();
            clone.Properties["resendId"] = 1; 
            await message.CompleteAsync();
            await _jetOrderRequests1.SendMesageAsync(clone);

            message = await _jetOrderRequests2.ReceiveMessageAsync();
            Assert.AreEqual(key, message.GetBody<JetOrderRequest>().JetVenueOrderId);
            Assert.AreEqual(1, message.Properties["resendId"]);
            await message.CompleteAsync();
        }

        [TestMethod]
        public async Task COMPLETE_CLONE_AND_RESEND_WITH_SCHEDUED_ENQUEUE_TIME()
        {
            var key = Guid.NewGuid().ToString();
            await _jetOrderRequests1.SendMesage(new JetOrderRequest() { JetVenueOrderId = key });

            var message = await _jetOrderRequests1.ReceiveMessageAsync();
            Assert.AreEqual(key, message.GetBody<JetOrderRequest>().JetVenueOrderId);
            var clone = message.Clone();
            clone.ScheduledEnqueueTimeUtc = DateTime.UtcNow.AddSeconds(30);
            clone.Properties["resendId"] = 1;
            await message.CompleteAsync();
            await _jetOrderRequests1.SendMesageAsync(clone);
            
            message = await _jetOrderRequests2.ReceiveMessageAsync(TimeSpan.FromSeconds(15));
            Assert.IsNull(message);

            message = await _jetOrderRequests2.ReceiveMessageAsync();
            Assert.AreEqual(key, message.GetBody<JetOrderRequest>().JetVenueOrderId);
            Assert.AreEqual(1, message.Properties["resendId"]);
            await message.CompleteAsync();
        }

        [TestMethod]
        public async Task COMPLETE_AND_RESEND_CLONE_IN_ROLLBACKED_TRANSACTION()
        {
            var key = Guid.NewGuid().ToString();
            await _jetOrderRequests1.SendMesage(new JetOrderRequest() { JetVenueOrderId = key });

            var message = await _jetOrderRequests1.ReceiveMessageAsync();
            Assert.AreEqual(key, message.GetBody<JetOrderRequest>().JetVenueOrderId);
            var clone = message.Clone();
            clone.Properties["resendId"] = 1;

            using (var scope = new TransactionScope())
            {
                message.Complete();
                _jetOrderRequests1.SendMesage(clone);
                // do not complete;
            }

            await Task.Delay(TimeSpan.FromMinutes(1));

            message = await _jetOrderRequests2.ReceiveMessageAsync();
            Assert.AreEqual(key, message.GetBody<JetOrderRequest>().JetVenueOrderId);
            Assert.IsFalse(message.Properties.ContainsKey("resendId"));
            await message.CompleteAsync();
            
            message = await _jetOrderRequests2.ReceiveMessageAsync(TimeSpan.FromMinutes(1));
            Assert.IsNull(message);
        }
    }
}
