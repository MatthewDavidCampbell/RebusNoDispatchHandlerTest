using System;

namespace Rebus.NoDispatchHandlers.Tests
{
    public class TimebombException: Exception 
    {
        public TimebombException(string messageId) {
            MessageId = messageId;
        }
        
        public string MessageId { get; }
    }
}