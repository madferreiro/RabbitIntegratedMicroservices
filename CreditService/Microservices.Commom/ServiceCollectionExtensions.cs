using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microservices.Common
{
    public static class ServiceCollectionExtensions
    {
        public static void SetupMicroservice(this IServiceCollection services, IConfiguration configuration, Action<DefaultMicroserviceBuilder> config)
        {
            var builder = new DefaultMicroserviceBuilder(services, configuration);
            config.Invoke(builder);
            builder.Build();
        }
    }
}
