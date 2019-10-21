using System;
using System.Collections.Generic;
using System.Linq;
using GreenPipes;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Sinks.Elasticsearch;
using Microservices.Common.MassTransit.Consumer;
using IConsumer = MassTransit.IConsumer;

namespace Microservices.Common.MassTransit
{
    public class RabbitMqConfigurator : Builder
    {
        private IServiceCollection Services { get; }

        private Dictionary<string, Func<LoggerConfiguration, LoggerConfiguration>> SetLogLevel { get; set; } = new Dictionary<string, Func<LoggerConfiguration, LoggerConfiguration>>()
        {
            { "Verbose", (lc) => { return lc.MinimumLevel.Verbose(); } },
            { "Debug", (lc) => { return lc.MinimumLevel.Debug(); } },
            { "Warning", (lc) => { return lc.MinimumLevel.Warning(); } },
            { "Error", (lc) => { return lc.MinimumLevel.Error(); } },
            { "Fatal", (lc) => { return lc.MinimumLevel.Fatal(); } }
        };


        private Uri RabbitMqUri { get; set; }

        private string RabbitMqUser { get; set; }

        private string RabbitMqPass { get; set; }

        private Serilog.Core.Logger Logger { get; set; }

        private string ExchangeType { get; set; } = RabbitMQ.Client.ExchangeType.Fanout;

        private bool IsDurable { get; set; }

        private IList<ReceiveEndpointDefinition> ReceiveEndpoints { get; set; } = new List<ReceiveEndpointDefinition>();

        public RabbitMqConfigurator(IServiceCollection services)
        {
            Services = services;
        }

        public RabbitMqConfigurator SetHost(string rabbitMqUri)
        {
            RabbitMqUri = new Uri(rabbitMqUri);
            return this;
        }

        public RabbitMqConfigurator SetLogging(string elasticEndpoint, string indexFormat, string elasticPassword, string logLevel)
        {
            var sinkOptions = new ElasticsearchSinkOptions(new Uri(elasticEndpoint))
            {
                AutoRegisterTemplate = true,
                IndexFormat = indexFormat,
                AutoRegisterTemplateVersion = AutoRegisterTemplateVersion.ESv6
            };

            if (!string.IsNullOrEmpty(elasticPassword))
            {
                sinkOptions.ModifyConnectionSettings = (c) => c.BasicAuthentication("elastic", elasticPassword);
            }
            var loggerConfig = new LoggerConfiguration();
            if (SetLogLevel.ContainsKey(logLevel))
                SetLogLevel[logLevel](loggerConfig);
            else
                loggerConfig.MinimumLevel.Error();
            loggerConfig.WriteTo.Elasticsearch(sinkOptions);
            Logger = loggerConfig.CreateLogger();
            return this;
        }

        public RabbitMqConfigurator SetHost(Uri rabbitMqUri)
        {
            RabbitMqUri = rabbitMqUri;
            return this;
        }

        public RabbitMqConfigurator SetCredentials(string rabbitMqUser, string rabbitMqPass)
        {
            RabbitMqUser = rabbitMqUser;
            RabbitMqPass = rabbitMqPass;
            return this;
        }

        public RabbitMqConfigurator SetExchangeType(string exchangeType)
        {
            ExchangeType = exchangeType;
            return this;
        }

        public RabbitMqConfigurator Durable(bool isDurable)
        {
            IsDurable = isDurable;
            return this;
        }

        public RabbitMqConfigurator AddReceiveEndpoint(string queueName,
            Action<ConsumerCollection> consumerConfigurationAction)
        {
            var recvEndpointDefinition = new ReceiveEndpointDefinition
            {
                Name = queueName
            };
            ReceiveEndpoints.Add(recvEndpointDefinition);

            consumerConfigurationAction.Invoke(recvEndpointDefinition.Consumers);

            return this;
        }

        public RabbitMqConfigurator AddReceiveEndpoint(string queueName, ConsumerCollection consumerCollection)
        {
            var recvEndpointDefinition = new ReceiveEndpointDefinition
            {
                Name = queueName,
                Consumers = consumerCollection
            };

            ReceiveEndpoints.Add(recvEndpointDefinition);
            return this;
        }

        public RabbitMqConfigurator AddReceiveEndpoint(ReceiveEndpointDefinition endpointDefinition)
        {
            ReceiveEndpoints.Add(endpointDefinition);
            return this;
        }

        internal override void Build()
        {
            Services.AddMassTransit(x =>
            {
                // Add consumers before registering them in receive endpoints
                var allConsumerTypes = ReceiveEndpoints.SelectMany(r => r.Consumers.GetCollectedConsumers());
                foreach (var consumerType in allConsumerTypes)
                {
                    x.AddConsumer(consumerType);
                }

                x.AddBus(provider => Bus.Factory.CreateUsingRabbitMq(sbc =>
                {
                    sbc.Host(RabbitMqUri, h =>
                    {
                        h.Username(RabbitMqUser);
                        h.Password(RabbitMqPass);
                    });

                    sbc.ExchangeType = ExchangeType;
                    sbc.Durable = IsDurable;

                    if (Logger != null)
                    {
                        sbc.UseSerilog(Logger);
                    }

                    var serviceProvider = Services.BuildServiceProvider();

                    foreach (var receiveEndpoint in ReceiveEndpoints)
                    {
                        sbc.ReceiveEndpoint(receiveEndpoint.Name, c =>
                        {
                            var consumerMethod = typeof(DependencyInjectionRegistrationExtensions)
                                .GetMethods().FirstOrDefault(m =>
                                    m.Name == "ConfigureConsumer" && m.IsStatic && m.IsGenericMethod &&
                                    m.GetParameters().Length == 3 &&
                                    m.GetParameters().Any(t => t.ParameterType.Name == "IServiceProvider"));

                            foreach (var consumerType in receiveEndpoint.Consumers.GetCollectedConsumers())
                            {
                                Services.AddTransient(consumerType);

                                var genericMethod = consumerMethod.MakeGenericMethod(consumerType);
                                genericMethod.Invoke(null, new object[] { c, serviceProvider, null });
                            }

                            c.UseRetry(r => r.Interval(6, TimeSpan.FromSeconds(10)));
                        });
                    }
                }));
            });
        }
    }
}
