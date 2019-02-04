
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Rebus.Retry;
using Rebus.Retry.Simple;

namespace Rebus.NoDispatchHandlers.Tests
{
    public class FakeErrorTracker : IErrorTracker
    {
        public FakeErrorTracker(SimpleRetryStrategySettings options) {
            Options = options;
        }

        public SimpleRetryStrategySettings Options { get; }

        public ConcurrentDictionary<string, List<Exception>> _exceptionCache = new ConcurrentDictionary<string, List<Exception>>();

        public void CleanUp(string messageId)
        {
            // Nothing
        }

        public IEnumerable<Exception> GetExceptions(string messageId)
        {
            return _exceptionCache.ContainsKey(messageId) ?
                _exceptionCache[messageId] :
                Enumerable.Empty<Exception>();
        }

        public string GetFullErrorDescription(string messageId)
        {
            return messageId;
        }

        public string GetShortErrorDescription(string messageId)
        {
            return messageId;
        }

        public bool HasFailedTooManyTimes(string messageId)
        {
            return _exceptionCache.ContainsKey(messageId) ?
                _exceptionCache[messageId].Count > Options.MaxDeliveryAttempts : 
                false;
        }

        public void MarkAsFinal(string messageId)
        {
            throw new NotImplementedException();
        }

        public void RegisterError(string messageId, Exception exception)
        {
            _exceptionCache.AddOrUpdate(
                messageId, 
                new List<Exception> { exception }, 
                (id, acc) => {
                    acc.Add(exception);
                    return acc;
                }
            );
        }
    }
}