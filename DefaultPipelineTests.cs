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
        /// <summary>
        /// Hard cords simple Timebomb Changed handler with
        /// a default pipeline
        /// </summary>
        /// <returns></returns>
        private static IServiceCollection Setup() {
            var services = new ServiceCollection();
            services.AddTransient<IHandleMessages<TimebombChangedEvent>, TimebombChangedHandler>();
            services.WithDefaultPipeline();

            return services;
        }

        /// <summary>
        /// Invoke the receive side of the default
        /// pipeline with the passed message
        /// </summary>
        /// <param name="provider"></param>
        /// <param name="id"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        private static async Task Invoke(
            IServiceProvider provider, 
            string id, 
            object message
        ) {
            var context = new FakeTransactionContext();

            var msg = new Message(
                new Dictionary<string, string>() { { Headers.MessageId, id } }, 
                message
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
            var services = Setup();
            var provider = services.BuildServiceProvider();

            // Act
            await Invoke(
                provider,
                "1",
                (ChangedEvent) new TimebombChangedEvent { isTimebomb = false }
            );

            // Assert
            var errorTracker = provider.GetService<IErrorTracker>();
            Assert.Empty(errorTracker.GetExceptions("1"));
        }

        [Fact]
        public async Task When_Invoke10MinimalPipelineWithDuds_ItShould_Complete()
        {
            // Arrange
            var services = Setup();
            var provider = services.BuildServiceProvider();

            // Act
            var calls = Enumerable.Range(1, 10)
                .Select(x => Invoke(
                    provider, 
                    x.ToString(),
                    (ChangedEvent) new TimebombChangedEvent { isTimebomb = false }
                ));
            await Task.WhenAll(calls);

            // Assert
            var errorTracker = provider.GetService<IErrorTracker>();
            Assert.Empty(errorTracker.GetExceptions("1"));
        }

        [Fact]
        public async Task When_Invoke10MinimalPipelineWithHalfBombs_ItShould_Complete()
        {
            // Arrange
            var services = Setup();
            var provider = services.BuildServiceProvider();

            // Act
            var calls = Enumerable.Range(1, 10)
                .Select(x => Invoke(
                    provider, 
                    x.ToString(),
                    (ChangedEvent) new TimebombChangedEvent { isTimebomb = x % 2 == 0 } 
                ));
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
            var services = Setup();
            var provider = services.BuildServiceProvider();

            // Act
            var calls = Enumerable.Range(1, howMany)
                .Select(x => Invoke(
                    provider, 
                    x.ToString(), 
                    (ChangedEvent) new TimebombChangedEvent { isTimebomb = x % 2 == 0 } 
                ));
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
