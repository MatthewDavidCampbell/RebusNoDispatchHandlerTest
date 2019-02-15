
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Rebus.Retry;
using Rebus.Retry.Simple;

namespace Rebus.NoDispatchHandlers.Tests.Fakes
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
            _exceptionCache.TryRemove(messageId, out var _);
        }

        public IEnumerable<Exception> GetExceptions(string messageId)
        {
            return _exceptionCache.ContainsKey(messageId) ?
                _exceptionCache[messageId] :
                Enumerable.Empty<Exception>();
        }

        public string GetFullErrorDescription(string messageId)
        {
            return GetShortErrorDescription(messageId);
        }

        public string GetShortErrorDescription(string messageId)
        {
            return _exceptionCache.TryGetValue(messageId, out var exceptions) ?
                $"{exceptions.Count} unhandled exceptions" :
                null;
        }

        public bool HasFailedTooManyTimes(string messageId)
        {
            return _exceptionCache.TryGetValue(messageId, out var exceptions) ?
                exceptions.Count > Options.MaxDeliveryAttempts : 
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