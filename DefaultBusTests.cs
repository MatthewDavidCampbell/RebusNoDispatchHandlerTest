using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Handlers;
using Rebus.NoDispatchHandlers.Tests.Exceptions;
using Rebus.NoDispatchHandlers.Tests.Fakes;
using Rebus.NoDispatchHandlers.Tests.Handlers;
using Rebus.Retry;
using Xunit;

namespace Rebus.NoDispatchHandlers.Tests
{
    public class DefaultBusTests {

        public const string DefaultQueue = "queue";

        /// <summary>
        /// Default services with a singleton for Timebomb changed 
        /// hanlder running against the default queue
        /// </summary>
        /// <param name="handler"></param>
        /// <returns></returns>
        public static IServiceCollection DefaultServices(
            IHandleMessages<TimebombChangedEvent> handler,
            int numberOfWorkers = 1
        ) {
            var services = new ServiceCollection();

            services.AddSingleton<Options>(new Options { NumberOfWorkers = numberOfWorkers });
            services.AddSingleton<IHandleMessages<TimebombChangedEvent>>(handler);

            services
                .WithDefaultPipeline(DefaultQueue)
                .UseInMemoryBus();
            return services;
        }

        /// <summary>
        /// Yield | start bus
        /// </summary>
        /// <param name="handler"></param>
        /// <returns></returns>
        public static IBus DefaultBus(
            IServiceProvider provider
        ) {
            var numberOfWorkers = provider.GetService<Options>().NumberOfWorkers;
            var bus = provider.GetService<RebusBus>();
            bus.Start(numberOfWorkers);

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
            var services = DefaultServices(handler);
            var provider = services.BuildServiceProvider();
            var bus = DefaultBus(provider);

            // Act
            var calls = Enumerable
                .Range(1, numberOfMessages)
                .Select(x => bus.Advanced.Routing.Send(DefaultQueue, new TimebombChangedEvent()));
            await Task.WhenAll(calls);

            await handler.WaitThresholdAsync();   

            // Assert
            Assert.Equal(numberOfMessages, handler.HandleCalled);
        }

        [Theory]
        [InlineData(100)]
        [InlineData(500)]
        [InlineData(1000)]
        public async Task When_SendingWithBombs_ItShould_Retry(int numberOfMessages)
        {
            // Arrange
            var handler = new TimebombChangedThresholdHandler(numberOfMessages * 6);
            var services = DefaultServices(handler);
            var provider = services.BuildServiceProvider();
            var bus = DefaultBus(provider);

            // Act
            var calls = Enumerable
                .Range(1, numberOfMessages)
                .Select(x => bus.Advanced.Routing.Send(
                    DefaultQueue, 
                    new TimebombChangedEvent { isTimebomb = true }
                ));
            await Task.WhenAll(calls);
            await handler.WaitThresholdAsync(); 

            // Assert
            Assert.Equal(numberOfMessages * 6, handler.HandleCalled);

            var errorTracker = provider.GetService<IErrorTracker>() as FakeErrorTracker;
            Assert.Equal(nameof(TimebombException),errorTracker.ExceptionKinds.Single().Name);
        }

        [Theory]
        [InlineData(100)]
        [InlineData(500)]
        [InlineData(1000)]
        public async Task When_SendingWithHalfBombs_ItShould_Retry(int numberOfMessages)
        {
            // Arrange
            var numberOfCalls = (numberOfMessages * 6 + numberOfMessages) / 2;
            var handler = new TimebombChangedThresholdHandler(numberOfCalls);
            var services = DefaultServices(handler, 2);
            var provider = services.BuildServiceProvider();
            var bus = DefaultBus(provider);

            // Act
            var calls = Enumerable
                .Range(1, numberOfMessages)
                .Select(x => bus.Advanced.Routing.Send(
                    DefaultQueue, 
                    new TimebombChangedEvent { isTimebomb = x % 2 == 0 }
                ));
            await Task.WhenAll(calls);
            await handler.WaitThresholdAsync(); 

            // Assert
            Assert.Equal(numberOfCalls, handler.HandleCalled);

            var errorTracker = provider.GetService<IErrorTracker>() as FakeErrorTracker;
            Assert.Equal(nameof(TimebombException),errorTracker.ExceptionKinds.Single().Name);
        }
    }
}