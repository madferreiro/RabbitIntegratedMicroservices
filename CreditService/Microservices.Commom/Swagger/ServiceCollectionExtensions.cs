using System;
using Microsoft.Extensions.DependencyInjection;

namespace Microservices.Common.Swagger
{
    public static class ServiceCollectionExtensions
    {
        public static void SetupSwaggerServices(this IServiceCollection services, Action<SwaggerServicesConfigurator> config)
        {
            var configurator = new SwaggerServicesConfigurator(services);
            config.Invoke(configurator);
            configurator.Build();
        }
    }
}
