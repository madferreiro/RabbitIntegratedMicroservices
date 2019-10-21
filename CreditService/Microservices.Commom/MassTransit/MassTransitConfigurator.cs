using System;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microservices.Common.MassTransit.HostedService;

namespace Microservices.Common.MassTransit
{
    public class MassTransitConfigurator
    {
        private IServiceCollection Services { get; }

        public MassTransitConfigurator(IServiceCollection services)
        {
            Services = services;

            Services.AddSingleton<IPublishEndpoint>(provider => provider.GetRequiredService<IBusControl>());
            Services.AddSingleton<ISendEndpointProvider>(provider => provider.GetRequiredService<IBusControl>());
            Services.AddSingleton<IBus>(provider => provider.GetRequiredService<IBusControl>());
            Services.AddSingleton<IHostedService, MassTransitBusService>();
        }

        public MassTransitConfigurator UseRabbitMqBusControl(Action<RabbitMqConfigurator> rabbitMqConfigurator)
        {
            var instance = new RabbitMqConfigurator(Services);
            rabbitMqConfigurator.Invoke(instance);
            instance.Build();

            return this;
        }
    }
}
