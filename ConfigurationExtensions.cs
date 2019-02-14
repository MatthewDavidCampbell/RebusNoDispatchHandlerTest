using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.DataBus;
using Rebus.Logging;
using Rebus.NoDispatchHandlers.Tests.Clones;
using Rebus.NoDispatchHandlers.Tests.Fakes;
using Rebus.Pipeline;
using Rebus.Pipeline.Receive;
using Rebus.Pipeline.Send;
using Rebus.Retry;
using Rebus.Retry.Simple;
using Rebus.Routing;
using Rebus.Routing.TypeBased;
using Rebus.Serialization;
using Rebus.Subscriptions;
using Rebus.Topic;
using Rebus.Transport;
using Rebus.Transport.InMem;
using Rebus.Workers;
using Rebus.Workers.ThreadPoolBased;

namespace Rebus.NoDispatchHandlers.Tests
{
    public static class ConfigurationExtensions {

        public static IServiceCollection WithDefaultPipeline(
            this IServiceCollection services,
            IHandlerActivator activator,
            string queueName
        ) {
            // handler activator
            services.AddSingleton<IHandlerActivator>(activator);

            // settings, error tracker + handler, logging, serializer
            services.AddSingleton<SimpleRetryStrategySettings>(new SimpleRetryStrategySettings {
                MaxDeliveryAttempts = 2,
                SecondLevelRetriesEnabled = true
            });
            services.AddSingleton<IErrorHandler, FakeErrorHandler>();
            services.AddSingleton<IErrorTracker, FakeErrorTracker>();
            services.AddSingleton<IRebusLoggerFactory, NullLoggerFactory>();
            services.AddSingleton<ISerializer, JsonSerializer>();

            // in-memory transport
            var transport = new InMemTransport(new InMemNetwork(true), queueName);
            services.AddSingleton<ITransport>(transport);

            // steps
            services.AddSingleton<IRetryStrategyStep, SimpleRetryStrategyStep>();

            // pipeline
            IPipeline PipelineFactory(IServiceProvider provider) {
                var pipeline = new DefaultPipeline()
                    .OnReceive(provider.GetRequiredService<IRetryStrategyStep>())
                    .OnReceive(new DeserializeIncomingMessageStep(provider.GetRequiredService<ISerializer>()))
                    .OnReceive(new ActivateHandlersStep(provider.GetRequiredService<IHandlerActivator>()))
                    .OnReceive(new DispatchIncomingMessageStep(provider.GetRequiredService<IRebusLoggerFactory>()))
                    
                    .OnSend(new AssignDefaultHeadersStep(provider.GetRequiredService<ITransport>()))
                    .OnSend(new AutoHeadersOutgoingStep())
                    .OnSend(new SerializeOutgoingMessageStep(provider.GetRequiredService<ISerializer>()))
                    .OnSend(new SendOutgoingMessageStep(provider.GetRequiredService<ITransport>(), provider.GetRequiredService<IRebusLoggerFactory>()));

                return pipeline;
            }
            services.AddSingleton<IPipeline>(PipelineFactory);

            // invoker
            services.AddSingleton<IPipelineInvoker>(provider => new DefaultPipelineInvoker(provider.GetRequiredService<IPipeline>()));

            return services;
        }

        public static IServiceCollection UseInMemoryBus(
            this IServiceCollection services
        ) {
            // backoff + options + lifetime events + router + subscription storage + data bus
            services.AddSingleton<IBackoffStrategy,FakeBackoffStrategy>();
            services.AddSingleton<Options>(new Options());
            services.AddSingleton<BusLifetimeEvents>(new BusLifetimeEvents());
            services.AddSingleton<IRouter>(provider => new TypeBasedRouter(provider.GetRequiredService<IRebusLoggerFactory>()));
            services.AddSingleton<ISubscriptionStorage>(new FakeSubscriptionStorage());
            services.AddSingleton<IDataBus>(new FakeDataBus());
            services.AddSingleton<ITopicNameConvention>(new DefaultTopicNameConvention());

            // default worker factory
            services.AddSingleton<IWorkerFactory>(provider => new ThreadPoolWorkerFactory(
                provider.GetRequiredService<ITransport>(),
                provider.GetRequiredService<IRebusLoggerFactory>(),
                provider.GetRequiredService<IPipelineInvoker>(),
                provider.GetRequiredService<Options>(),
                provider.GetRequiredService<RebusBus>,
                provider.GetRequiredService<BusLifetimeEvents>(),
                provider.GetRequiredService<IBackoffStrategy>()
            ));

            services.AddSingleton<RebusBus>(provider => new RebusBus(
                provider.GetRequiredService<IWorkerFactory>(),
                provider.GetRequiredService<IRouter>(),
                provider.GetRequiredService<ITransport>(),
                provider.GetRequiredService<IPipelineInvoker>(),
                provider.GetRequiredService<ISubscriptionStorage>(),
                provider.GetRequiredService<Options>(),
                provider.GetRequiredService<IRebusLoggerFactory>(),
                provider.GetRequiredService<BusLifetimeEvents>(),
                provider.GetRequiredService<IDataBus>(),
                provider.GetRequiredService<ITopicNameConvention>()
            ));

            services.AddSingleton<IBus>(provider => provider.GetRequiredService<RebusBus>());

            return services;
        }
    }
}