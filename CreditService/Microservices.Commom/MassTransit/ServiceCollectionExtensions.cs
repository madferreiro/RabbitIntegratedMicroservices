using Microsoft.Extensions.DependencyInjection;

namespace Microservices.Common.MassTransit
{
    public static class ServiceCollectionExtensions
    {
        public static MassTransitConfigurator SetupMassTransitServices(this IServiceCollection services)
        {
            var configurator = new MassTransitConfigurator(services);
            return configurator;
        }
    }
}
