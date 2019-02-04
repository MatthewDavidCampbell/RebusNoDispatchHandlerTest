using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Rebus.Transport;

namespace Rebus.NoDispatchHandlers.Tests
{
    public class FakeTransactionContext : ITransactionContext
    {
        public FakeTransactionContext()
        {
            Items = new ConcurrentDictionary<string, object>();
        }
        public void Dispose()
        {
            // Nothing
        }

        public ConcurrentDictionary<string, object> Items { get; }

        public void OnCommitted(Func<Task> commitAction)
        {
            throw new NotImplementedException();
        }

        public void OnAborted(Action abortedAction)
        {
            throw new NotImplementedException();
        }

        public void OnCompleted(Func<Task> completedAction)
        {
            throw new NotImplementedException();
        }

        public void OnDisposed(Action disposedAction)
        {
            disposedAction();
        }

        public void Abort()
        {
            // nothing
        }

        public Task Commit()
        {
            return Task.CompletedTask;
        }
    }
}