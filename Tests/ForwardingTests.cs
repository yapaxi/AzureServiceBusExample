using AzureServiceBusExample;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests
{
    [TestClass]
    public class ForwardingTests
    {
        private static NamespaceManager _ns;
        private static MessagingFactory _messagingFactory;

        private static string NamespaceFor(params string[] subpath) => typeof(ForwardingTests).FullName + "." + string.Join(".", subpath);

        [ClassInitialize]
        public static void Init(TestContext context)
        {
            var connectionString = Program.GetConnectionString();
            _ns = NamespaceManager.CreateFromConnectionString(connectionString);
            _messagingFactory = MessagingFactory.CreateFromConnectionString(connectionString);
        }

        [TestMethod]
        public void AUTOFORWARDING_TREE()
        {
            var root = new TopicDescription(NamespaceFor(nameof(AUTOFORWARDING_TREE), "v3")).GetOrCreate(_ns);
            var leaves = CreateTreeForRange(root.Path, 1, 0, 50).ToArray();

            var x = 20;

            var leave = leaves.Where(e => e.Name == $"LEAVE-{x}").Single();

            var rootTopicClient = _messagingFactory.CreateTopicClient(root.Path);
            var leaveSubsriptionClient = _messagingFactory.CreateSubscriptionClient(leave.TopicPath, leave.Name);

            var msg = new BrokeredMessage();
            msg.Properties["i"] = x;
            rootTopicClient.Send(msg);

            msg = leaveSubsriptionClient.Receive();
            Assert.AreEqual(x, msg.Properties["i"]);
        }

        private static IEnumerable<SubscriptionDescription> CreateTreeForRange(string rootPath, int level, int min, int max)
        {
            if (max - min <= 10)
            {
                for (int i = min; i <= max; i++)
                {
                    var s = new SubscriptionDescription(rootPath, $"LEAVE-{i}")
                        .GetOrCreate(_ns, new SqlFilter($"i = {i}"));
                    yield return s;
                }
            }
            else
            {
                var m = (max + min) / 2;

                var lt = new TopicDescription($"{rootPath}-{min}_{m}").Defaults().GetOrCreate(_ns);
                var rt = new TopicDescription($"{rootPath}-{m+1}_{max}").Defaults().GetOrCreate(_ns);

                var ls = new SubscriptionDescription(rootPath, $"LEFT{level}")
                    .Setup(forwardTo: lt.Path)
                    .GetOrCreate(_ns, new SqlFilter($"i < {m}"));

                var rs = new SubscriptionDescription(rootPath, $"RIGHT{level}")
                    .Setup(forwardTo: rt.Path)
                    .GetOrCreate(_ns, new SqlFilter($"i >= {m}"));

                foreach (var s in CreateTreeForRange(lt.Path, level + 1, min, m).Concat(
                                  CreateTreeForRange(rt.Path, level + 1, m + 1, max)))
                {
                    yield return s;
                }
            }
        }
    }

    public static class Extentions
    {
        public static SubscriptionDescription Setup(this SubscriptionDescription subs, string forwardTo)
        {
            subs.AutoDeleteOnIdle = TimeSpan.FromHours(1);
            subs.ForwardTo = forwardTo;
            return subs;
        }

        public static TopicDescription Defaults(this TopicDescription topic)
        {
            topic.AutoDeleteOnIdle = TimeSpan.FromHours(1);
            return topic;
        }

        public static SubscriptionDescription GetOrCreate(this SubscriptionDescription subs, NamespaceManager ns, SqlFilter filter)
        {
            if (ns.SubscriptionExists(subs.TopicPath, subs.Name))
            {
                return ns.GetSubscription(subs.TopicPath, subs.Name);
            }
            else
            {
                return ns.CreateSubscription(subs, filter);
            }
        }
        
        public static TopicDescription GetOrCreate(this TopicDescription topic, NamespaceManager ns)
        {
            if (ns.TopicExists(topic.Path))
            {
                return ns.GetTopic(topic.Path);
            }
            else
            {
                return ns.CreateTopic(topic.Path);
            }
        }
    }

}
