using System;

namespace Rebus.NoDispatchHandlers.Tests.Exceptions
{
    public class TimebombException: Exception 
    {
        public TimebombException(string messageId) {
            MessageId = messageId;
        }
        
        public string MessageId { get; }
    }
}