using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Rebus.Activation;
using Rebus.Extensions;
using Rebus.Handlers;
using Rebus.Transport;

/// <summary>
/// NOTE: Copied from the Rebus repo (latest)
/// </summary>
namespace Rebus.NoDispatchHandlers.Tests.Clones
{
    public class DependencyInjectionHandlerActivator : IHandlerActivator
    {
        readonly IServiceProvider _provider;

        /// <summary>
        /// Initializes a new instance of the <see cref="DependencyInjectionHandlerActivator"/> class.
        /// </summary>
        /// <param name="provider">The service provider used to yield handler instances.</param>
        public DependencyInjectionHandlerActivator(IServiceProvider provider)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        /// <summary>
        /// Resolves all handlers for the given <typeparamref name="TMessage"/> message type
        /// </summary>
        /// <exception cref="System.InvalidOperationException"></exception>
        public Task<IEnumerable<IHandleMessages<TMessage>>> GetHandlers<TMessage>(TMessage message, ITransactionContext transactionContext)
        {
            var scope = _provider.CreateScope();

            var resolvedHandlerInstances = GetMessageHandlersForMessage<TMessage>(scope);

            transactionContext.OnDisposed(scope.Dispose);

            return Task.FromResult((IEnumerable<IHandleMessages<TMessage>>)resolvedHandlerInstances.ToArray());
        }
        
        List<IHandleMessages<TMessage>> GetMessageHandlersForMessage<TMessage>(IServiceScope scope)
        {
            var handledMessageTypes = typeof(TMessage).GetBaseTypes()
                .Concat(new[] { typeof(TMessage) });

            return handledMessageTypes
                .SelectMany(t =>
                {
                    var implementedInterface = typeof(IHandleMessages<>).MakeGenericType(t);

                    return scope.ServiceProvider.GetServices(implementedInterface).Cast<IHandleMessages>();
                })
                .Cast<IHandleMessages<TMessage>>()
                .ToList();
        }
    }
}