using System;
using System.Threading.Tasks;
using Rebus.Bus;
using Rebus.Handlers;
using Rebus.Pipeline;

namespace Rebus.NoDispatchHandlers.Tests
{
    public class FakeMessageHandler : IHandleMessages<FakeMessage>
    {
        public Task Handle(FakeMessage message)
        {
            if (message.isTimebomb) {
                throw new TimebombException(MessageContext.Current.TransportMessage.GetMessageId());
            }

            return Task.CompletedTask;
        }
    }
}