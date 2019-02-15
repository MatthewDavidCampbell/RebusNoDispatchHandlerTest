using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Handlers;
using Rebus.NoDispatchHandlers.Tests.Handlers;
using Xunit;

namespace Rebus.NoDispatchHandlers.Tests
{
    public class DefaultBusTests {

        public const string DefaultQueue = "queue";

        /// <summary>
        /// Yield a bus with a default pipeline receiving from 
        /// the default queue using a singleton handler for 
        /// Timebomb changes
        /// </summary>
        /// <param name="handler"></param>
        /// <returns></returns>
        public static IBus DefaultBus(
            IHandleMessages<TimebombChangedEvent> handler
        ) {
            var services = new ServiceCollection();

            services.AddSingleton<IHandleMessages<TimebombChangedEvent>>(handler);

            services
                .WithDefaultPipeline(DefaultQueue)
                .UseInMemoryBus();
            var provider = services.BuildServiceProvider();

            var bus = provider.GetService<RebusBus>();
            bus.Start(1);

            return bus;
        }


        [Theory]
        [InlineData(100)]
        [InlineData(500)]
        [InlineData(1000)]
        public async Task When_SendingWithDuds_ItShould_Complete(int numberOfMessages)
        {
            // Arrange
            var handler = new TimebombChangedThresholdHandler(numberOfMessages);
            var bus = DefaultBus(handler);

            // Act
            var calls = Enumerable
                .Range(1, numberOfMessages)
                .Select(x => bus.Advanced.Routing.Send(DefaultQueue, new TimebombChangedEvent()));
            await Task.WhenAll(calls);

            await handler.WaitThresholdAsync();   

            // Assert
            Assert.Equal(numberOfMessages, handler.HandleCalled);
        }
    }
}