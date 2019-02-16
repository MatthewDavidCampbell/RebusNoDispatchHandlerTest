using System;
using System.Threading;
using System.Threading.Tasks;
using Rebus.Bus;
using Rebus.Handlers;
using Rebus.NoDispatchHandlers.Tests.Exceptions;
using Rebus.Pipeline;

namespace Rebus.NoDispatchHandlers.Tests.Handlers
{
    public class MultipleThresholdHandler : IHandleMessages<TimebombChangedEvent>, IHandleMessages<ChangedEvent>
    {
        private int _handleCalled;

        private EventWaitHandle _whenThreshold = new ManualResetEvent(false);

        public MultipleThresholdHandler(int threshold) {
            Threshold = threshold;
        }

        public int HandleCalled => _handleCalled;

        public int Threshold { get; }

        public Task Handle(TimebombChangedEvent message)
        {
            if (Interlocked.Increment(ref _handleCalled) >= Threshold) {
                _whenThreshold.Set();
            }

            if (message.isTimebomb) {
                throw new TimebombException(MessageContext.Current.TransportMessage.GetMessageId());
            }

            return Task.CompletedTask;
        }

        public Task Handle(ChangedEvent message) {
            if (Interlocked.Increment(ref _handleCalled) >= Threshold) {
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