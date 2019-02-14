using System;
using System.Threading;
using System.Threading.Tasks;
using Rebus.Bus;
using Rebus.Handlers;
using Rebus.NoDispatchHandlers.Tests.Exceptions;
using Rebus.Pipeline;

namespace Rebus.NoDispatchHandlers.Tests.Handlers
{
    public class TimebombChangedThresholdHandler : IHandleMessages<TimebombChangedEvent>
    {
        private int _handleCalled;

        private EventWaitHandle _whenThreshold = new ManualResetEvent(false);

        public TimebombChangedThresholdHandler(int threshold) {
            Threshold = threshold;
        }

        public int HandleCalled => _handleCalled;

        public int Threshold { get; }

        public Task Handle(TimebombChangedEvent message)
        {
            Interlocked.Increment(ref _handleCalled);

            if (message.isTimebomb) {
                throw new TimebombException(MessageContext.Current.TransportMessage.GetMessageId());
            }

            if (Thread.VolatileRead(ref _handleCalled) >= Threshold) {
                _whenThreshold.Set();
            }

            return Task.CompletedTask;
        }

        public async Task WaitThresholdAsync(
            CancellationToken cancellationToken = default(CancellationToken)
        ) {
            await _whenThreshold.WaitOneAsync(cancellationToken);
        }
    }
}