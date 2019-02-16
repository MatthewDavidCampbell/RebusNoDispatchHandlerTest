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
using Rebus.Retry.Simple;
using Xunit;

namespace Rebus.NoDispatchHandlers.Tests
{
    public class DefaultBusTests {

        public const string DefaultQueue = "queue";

        public static SimpleRetryStrategySettings SimpleRetryStrategySettings = new SimpleRetryStrategySettings {
            MaxDeliveryAttempts = 2,
            SecondLevelRetriesEnabled = true
        };

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

            services.AddSingleton<Options>(new Options { 
                NumberOfWorkers = numberOfWorkers 
            });
            services.AddSingleton<SimpleRetryStrategySettings>(SimpleRetryStrategySettings);
            services.AddSingleton<IHandleMessages<TimebombChangedEvent>>(handler);
            services.AddTransient<ISomeDependency, SomeDependency>();
            services.AddTransient<IHandleMessages<ChangedEvent>, ChangedHandler>();

            services
                .WithDefaultPipeline(DefaultQueue)
                .UseInMemoryBus();
            return services;
        }

        public static IServiceCollection MultiHanlderServices(
            IHandleMessages<TimebombChangedEvent> handler1,
            IHandleMessages<ChangedEvent> handler2,
            int numberOfWorkers = 1
        ) {
            var services = new ServiceCollection();

            services.AddSingleton<Options>(new Options { 
                NumberOfWorkers = numberOfWorkers 
            });
            services.AddSingleton<SimpleRetryStrategySettings>(SimpleRetryStrategySettings);
            services.AddSingleton<IHandleMessages<TimebombChangedEvent>>(handler1);
            services.AddSingleton<IHandleMessages<ChangedEvent>>(handler2);

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
        public async Task When_SendingWithHalfBombs_ItShould_Retry(int numberOfMessages)
        {
            // Arrange
            var numberOfTimebombCalls = (numberOfMessages * SimpleRetryStrategySettings.MaxDeliveryAttempts * 2 + numberOfMessages) / 2;
            var handler = new TimebombChangedThresholdHandler(numberOfTimebombCalls);
            var services = DefaultServices(handler, 2);
            var provider = services.BuildServiceProvider();
            var bus = DefaultBus(provider);

            // Act
            var calls = Enumerable
                .Range(1, numberOfMessages)
                .Select(x => bus.Advanced.Routing.Send(
                    DefaultQueue, 
                    (ChangedEvent) new TimebombChangedEvent { isTimebomb = x % 2 == 0 }
                ));
            await Task.WhenAll(calls);
            await handler.WaitThresholdAsync(); 

            // Assert
            Assert.Equal(numberOfTimebombCalls, handler.HandleCalled);

            var errorTracker = provider.GetService<IErrorTracker>() as FakeErrorTracker;
            Assert.Equal(nameof(TimebombException),errorTracker.ExceptionKinds.Single().Name);
        }

        [Theory]
        [InlineData(100)]
        [InlineData(500)]
        [InlineData(1000)]
        public async Task When_SendingWithBombsAndChanged_ItShould_Retry(int numberOfMessages)
        {
            // Arrange
            var numberOfTimebombCalls = (numberOfMessages * SimpleRetryStrategySettings.MaxDeliveryAttempts * 2) / 2;
            var handler = new TimebombChangedThresholdHandler(numberOfTimebombCalls);
            var services = DefaultServices(handler, 2);
            var provider = services.BuildServiceProvider();
            var bus = DefaultBus(provider);

            // Act
            var calls = Enumerable
                .Range(1, numberOfMessages)
                .Select(x => bus.Advanced.Routing.Send(
                    DefaultQueue,
                    x % 2 == 0 ? 
                        new ChangedEvent {} :
                        new TimebombChangedEvent { isTimebomb = true } 
                ));
            await Task.WhenAll(calls);
            await handler.WaitThresholdAsync(); 

            // Assert
            Assert.Equal(numberOfTimebombCalls, handler.HandleCalled);

            var errorTracker = provider.GetService<IErrorTracker>() as FakeErrorTracker;
            Assert.Equal(nameof(TimebombException),errorTracker.ExceptionKinds.Single().Name);
        }

        [Theory]
        [InlineData(100)]
        [InlineData(500)]
        [InlineData(1000)]
        public async Task When_SendingAtMultipleHandler_ItShould_Retry(int numberOfMessages)
        {
            // Arrange
            var numberOfCalls = (numberOfMessages * SimpleRetryStrategySettings.MaxDeliveryAttempts) + (numberOfMessages / 2);
            var handler = new MultipleThresholdHandler(numberOfCalls);
            var services = MultiHanlderServices(handler, handler);
            var provider = services.BuildServiceProvider();
            var bus = DefaultBus(provider);

            // Act
            var calls = Enumerable
                .Range(1, numberOfMessages)
                .Select(x => bus.Advanced.Routing.Send(
                    DefaultQueue,
                    x % 2 == 0 ? 
                        new ChangedEvent {} :
                        new TimebombChangedEvent { isTimebomb = true } 
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