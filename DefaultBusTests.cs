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

        public static IBus DefaultBus(
            IHandleMessages<TimebombChangedEvent> handler
        ) {
            var services = new ServiceCollection()
                .WithDefaultPipeline(new BuiltinHandlerActivator().Register(() => handler), "queue")
                .UseInMemoryBus();
            var provider = services.BuildServiceProvider();

            var bus = provider.GetService<RebusBus>();
            bus.Start(1);

            return bus;
        }


        [Fact]
        public async Task When_SendingWithDuds_ItShould_Complete()
        {
            // Arrange
            var numberOfMessages = 10;
            var handler = new TimebombChangedThresholdHandler(numberOfMessages);
            var bus = DefaultBus(handler);

            // Act
            var calls = Enumerable
                .Range(1, numberOfMessages)
                .Select(x => bus.Advanced.Routing.Send("queue", new TimebombChangedEvent()));
            await Task.WhenAll(calls);

            await handler.WaitThresholdAsync();   

            // Assert
            Assert.Equal(numberOfMessages, handler.HandleCalled);
        }
    }
}