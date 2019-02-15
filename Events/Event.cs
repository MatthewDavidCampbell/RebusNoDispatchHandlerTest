using System;

namespace Rebus.NoDispatchHandlers.Tests
{
    public class Event: IEvent {
        public DateTime CreatedUtcTime { get; set; } = DateTime.UtcNow;

        public Guid MessageId { get; set; } = Guid.NewGuid();

        public Guid CorrelationId { get; set; }
    }
}