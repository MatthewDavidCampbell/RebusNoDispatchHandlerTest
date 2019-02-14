using System.Threading.Tasks;
using Rebus.Subscriptions;

namespace Rebus.NoDispatchHandlers.Tests.Fakes
{
    public class FakeSubscriptionStorage : ISubscriptionStorage
    {
        public bool IsCentralized => throw new System.NotImplementedException();

        public Task<string[]> GetSubscriberAddresses(string topic)
        {
            throw new System.NotImplementedException();
        }

        public Task RegisterSubscriber(string topic, string subscriberAddress)
        {
            throw new System.NotImplementedException();
        }

        public Task UnregisterSubscriber(string topic, string subscriberAddress)
        {
            throw new System.NotImplementedException();
        }
    }
}