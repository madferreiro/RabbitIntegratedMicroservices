using Microsoft.Extensions.DependencyInjection;
using Microservices.Common.MassTransit.Consumer;

namespace Microservices.Common.MassTransit
{
    public class ReceiveEndpointConfigurator : Builder
    {
        private IServiceCollection Services { get; }

        private ConsumerCollection Consumers { get; }

        public ReceiveEndpointConfigurator(IServiceCollection services, ConsumerCollection consumerCollection)
        {
            Services = services;
            Consumers = consumerCollection;
        }

        internal override void Build()
        {

        }
    }
}
