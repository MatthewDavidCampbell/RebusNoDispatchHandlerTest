using System.Threading;
using System.Threading.Tasks;
using Rebus.Workers.ThreadPoolBased;

namespace Rebus.NoDispatchHandlers.Tests.Fakes
{
    public class FakeBackoffStrategy : IBackoffStrategy
    {
        public void Reset()
        {
            
        }

        public void Wait(CancellationToken token)
        {
            
        }

        public Task WaitAsync()
        {
            return Task.CompletedTask;
        }

        public void WaitError()
        {
           
        }

        public Task WaitErrorAsync(CancellationToken token)
        {
           return Task.CompletedTask;
        }

        public void WaitNoMessage()
        {

        }

        public Task WaitNoMessageAsync(CancellationToken token)
        {
            return Task.CompletedTask;
        }
    }
}