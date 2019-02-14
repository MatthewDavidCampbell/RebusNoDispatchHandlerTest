using System;

namespace Rebus.NoDispatchHandlers.Tests
{
    public interface IMessage {
        Guid MessageId { get; set; }

        DateTime CreatedUtcTime { get; set; }

        Guid CorrelationId { get; set; }
    }
}