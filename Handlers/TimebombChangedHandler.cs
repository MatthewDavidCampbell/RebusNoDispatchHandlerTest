using System;
using System.Threading.Tasks;
using Rebus.Bus;
using Rebus.Handlers;
using Rebus.NoDispatchHandlers.Tests.Exceptions;
using Rebus.Pipeline;

namespace Rebus.NoDispatchHandlers.Tests.Handlers
{
    public class TimebombChangedHandler : IHandleMessages<TimebombChangedEvent>
    {
        public Task Handle(TimebombChangedEvent message)
        {
            if (message.isTimebomb) {
                throw new TimebombException(MessageContext.Current.TransportMessage.GetMessageId());
            }

            return Task.CompletedTask;
        }
    }
}