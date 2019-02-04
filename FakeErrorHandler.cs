using System;
using System.Threading.Tasks;
using Rebus.Messages;
using Rebus.Retry;
using Rebus.Transport;

namespace Rebus.NoDispatchHandlers.Tests
{
    public class FakeErrorHandler : IErrorHandler
    {
        public Task HandlePoisonMessage(TransportMessage transportMessage, ITransactionContext transactionContext, Exception exception)
        {
            return Task.CompletedTask;
        }
    }
}