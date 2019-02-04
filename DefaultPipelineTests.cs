using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Rebus.Activation;
using Rebus.Handlers;
using Rebus.Logging;
using Rebus.Messages;
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
        public static Func<IServiceCollection> DefaultServices = () => {
            var services = new ServiceCollection();

            // activator (handlers)
            var activator = new BuiltinHandlerActivator();
            activator.Register(() => (FakeMessageHandler) Activator.CreateInstance(typeof(FakeMessageHandler)));
            services.AddSingleton<IHandlerActivator>(activator);

            // settings, error tracker + handler, logging, serializer
            services.AddSingleton<SimpleRetryStrategySettings>(new SimpleRetryStrategySettings {
                MaxDeliveryAttempts = 2
            });
            services.AddSingleton<IErrorHandler, FakeErrorHandler>();
            services.AddSingleton<IErrorTracker, FakeErrorTracker>();
            services.AddSingleton<IRebusLoggerFactory, NullLoggerFactory>();
            services.AddSingleton<ISerializer, JsonSerializer>();

            // steps
            services.AddSingleton<IRetryStrategyStep, SimpleRetryStrategyStep>();

            // pipeline
            IPipeline PipelineFactory(IServiceProvider provider) {
                var pipeline = new DefaultPipeline()
                    .OnReceive(provider.GetRequiredService<IRetryStrategyStep>())
                    .OnReceive(new DeserializeIncomingMessageStep(provider.GetRequiredService<ISerializer>()))
                    .OnReceive(new ActivateHandlersStep(provider.GetRequiredService<IHandlerActivator>()))
                    .OnReceive(new DispatchIncomingMessageStep(provider.GetRequiredService<IRebusLoggerFactory>()));

                return pipeline;
            }
            services.AddSingleton<IPipeline>(PipelineFactory);

            // invoker
            services.AddSingleton<IPipelineInvoker>(provider => new DefaultPipelineInvoker(provider.GetRequiredService<IPipeline>()));

            return services;
        };

        public static async Task Invoke(ServiceProvider provider, string id, bool isTimebomb = false) {
            var context = new FakeTransactionContext();

            var msg = new Message(
                new Dictionary<string, string>() { { Headers.MessageId, id } }, 
                new FakeMessage { isTimebomb = isTimebomb }
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
        public async Task When_InvokeMinimalPipelineWithoutBomb_ItShould_Complete()
        {
            // Arrange
            var services = DefaultServices();
            var provider = services.BuildServiceProvider();

            // Act
            await Invoke(provider, "1");

            // Assert
            var errorTracker = provider.GetService<IErrorTracker>();
            Assert.Empty(errorTracker.GetExceptions("1"));
        }

        [Fact]
        public async Task When_Invoke10MinimalPipelineWithoutBomb_ItShould_Complete()
        {
            // Arrange
            var services = DefaultServices();
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
            var services = DefaultServices();
            var provider = services.BuildServiceProvider();

            // Act
            var calls = Enumerable.Range(1, 10)
                .Select(x => Invoke(provider, x.ToString(), x % 2 == 0));
            await Task.WhenAll(calls);

            // Assert
            var errorTracker = provider.GetService<IErrorTracker>();
            var distinctExceptionsCount = Enumerable.Range(1, 10)
                .Where(x => x % 2 == 0)
                .Select(x => errorTracker.GetExceptions(x.ToString()).First().GetType())
                .GroupBy(x => x.Name)
                .Count();
            Assert.Equal(1, distinctExceptionsCount);
        }

        [Theory]
        [InlineData(100)]
        [InlineData(500)]
        [InlineData(1000)]
        public async Task When_InvokeSomeMinimalPipelineWithHalfBombs_ItShould_Complete(int howMany)
        {
            // Arrange
            var services = DefaultServices();
            var provider = services.BuildServiceProvider();

            // Act
            var calls = Enumerable.Range(1, howMany)
                .Select(x => Invoke(provider, x.ToString(), x % 2 == 0));
            await Task.WhenAll(calls);

            // Assert
            var errorTracker = provider.GetService<IErrorTracker>();
            var distinctExceptionsCount = Enumerable.Range(1, howMany)
                .Where(x => x % 2 == 0)
                .Select(x => errorTracker.GetExceptions(x.ToString()).First().GetType())
                .GroupBy(x => x.Name)
                .Count();
            Assert.Equal(1, distinctExceptionsCount);
        }
    }
}
