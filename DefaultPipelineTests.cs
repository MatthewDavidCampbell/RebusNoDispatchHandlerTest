using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Rebus.Activation;
using Rebus.Handlers;
using Rebus.Logging;
using Rebus.Messages;
using Rebus.NoDispatchHandlers.Tests.Clones;
using Rebus.NoDispatchHandlers.Tests.Exceptions;
using Rebus.NoDispatchHandlers.Tests.Fakes;
using Rebus.NoDispatchHandlers.Tests.Handlers;
using Rebus.Pipeline;
using Rebus.Pipeline.Receive;
using Rebus.Retry;
using Rebus.Retry.Simple;
using Rebus.Serialization;
using Rebus.Transport;
using Xunit;

namespace Rebus.NoDispatchHandlers.Tests
{
    public class DefaultPipelineTests
    {
        private static Func<TimebombChangedHandler> TimebombChangedHandlerFactory = () => (TimebombChangedHandler) Activator.CreateInstance(typeof(TimebombChangedHandler));

        private static IHandlerActivator DefaultHandlerActivator = new BuiltinHandlerActivator().Register(TimebombChangedHandlerFactory);

        private static async Task Invoke(ServiceProvider provider, string id, bool isTimebomb = false) {
            var context = new FakeTransactionContext();

            var msg = new Message(
                new Dictionary<string, string>() { { Headers.MessageId, id } }, 
                (ChangedEvent) new TimebombChangedEvent { isTimebomb = isTimebomb }
            );

            var serializer = provider.GetService<ISerializer>();
            var invoker = provider.GetService<IPipelineInvoker>();

            AmbientTransactionContext.SetCurrent(context);

            var transportMessage = await serializer.Serialize(msg);
            var ctx = new IncomingStepContext(transportMessage, context);

            await invoker.Invoke(ctx);

            context.Dispose();
        }

        [Fact]
        public async Task When_InvokeMinimalPipelineWithDuds_ItShould_Complete()
        {
            // Arrange
            var services = new ServiceCollection().WithDefaultPipeline(DefaultHandlerActivator, "queue");
            var provider = services.BuildServiceProvider();

            // Act
            await Invoke(provider, "1");

            // Assert
            var errorTracker = provider.GetService<IErrorTracker>();
            Assert.Empty(errorTracker.GetExceptions("1"));
        }

        [Fact]
        public async Task When_Invoke10MinimalPipelineWithDuds_ItShould_Complete()
        {
            // Arrange
            var services = new ServiceCollection().WithDefaultPipeline(DefaultHandlerActivator, "queue");
            var provider = services.BuildServiceProvider();

            // Act
            var calls = Enumerable.Range(1, 10)
                .Select(x => Invoke(provider, x.ToString()));
            await Task.WhenAll(calls);

            // Assert
            var errorTracker = provider.GetService<IErrorTracker>();
            Assert.Empty(errorTracker.GetExceptions("1"));
        }

        [Fact]
        public async Task When_Invoke10MinimalPipelineWithHalfBombs_ItShould_Complete()
        {
            // Arrange
            var services = new ServiceCollection().WithDefaultPipeline(DefaultHandlerActivator, "queue");
            var provider = services.BuildServiceProvider();

            // Act
            var calls = Enumerable.Range(1, 10)
                .Select(x => Invoke(provider, x.ToString(), x % 2 == 0));
            await Task.WhenAll(calls);

            // Assert
            var errorTracker = provider.GetService<IErrorTracker>();
            var invoker = provider.GetService<IPipelineInvoker>() as DefaultPipelineInvoker;
            var distinctExceptions = Enumerable.Range(1, 10)
                .Where(x => x % 2 == 0)
                .Select(x => errorTracker.GetExceptions(x.ToString()).First().GetType())
                .GroupBy(x => x.Name)
                .Select(x => x.Key);

            Assert.Equal(nameof(TimebombException), distinctExceptions.Single());
            Assert.Equal(5, invoker.NumberOfCompleted);
        }

        [Theory]
        [InlineData(100)]
        [InlineData(500)]
        [InlineData(1000)]
        public async Task When_InvokeSomeMinimalPipelineWithHalfBombs_ItShould_Complete(int howMany)
        {
            // Arrange
            var services = new ServiceCollection().WithDefaultPipeline(DefaultHandlerActivator, "queue");
            var provider = services.BuildServiceProvider();

            // Act
            var calls = Enumerable.Range(1, howMany)
                .Select(x => Invoke(provider, x.ToString(), x % 2 == 0));
            await Task.WhenAll(calls);

            // Assert
            var errorTracker = provider.GetService<IErrorTracker>();
            var invoker = provider.GetService<IPipelineInvoker>() as DefaultPipelineInvoker;
            var distinctExceptions = Enumerable.Range(1, howMany)
                .Where(x => x % 2 == 0)
                .Select(x => errorTracker.GetExceptions(x.ToString()).First().GetType())
                .GroupBy(x => x.Name)
                .Select(x => x.Key);

            Assert.Equal(nameof(TimebombException), distinctExceptions.Single());
            Assert.Equal((int)(howMany / 2), invoker.NumberOfCompleted);
        }
    }
}
