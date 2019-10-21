using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using RabbitMQ.Client;
using Microservices.Common.MassTransit;
using Microservices.Common.MassTransit.Consumer;
using Microservices.Common.Swagger;

namespace Microservices.Common
{
    public class DefaultMicroserviceBuilder : Builder
    {
        private IServiceCollection Services { get; }

        private IConfiguration Configuration { get; }

        private readonly List<ReceiveEndpointDefinition> _receiveEndpointDefinitions =
            new List<ReceiveEndpointDefinition>();

        public DefaultMicroserviceBuilder(IServiceCollection services, IConfiguration configuration)
        {
            Services = services;
            Configuration = configuration;
        }

        public string ServiceName { get; set; }

        public Version ServiceVersion { get; set; }

        public bool SetupMassTransit { get; set; }

        public bool SetupDefaultReceiveEndpoint { get; set; }

        public bool UseDefaultSwaggerSetup { get; set; } = true;

        public bool UseCors { get; set; } = true;

        public bool UseJwtAuthentication { get; set; } = true;

        public IList<string> CorsAllowedHosts { get; set; } = new List<string>
        {
            "http://localhost:8080", "http://localhost:81"
        };

        // MassTransit/RabbitMq

        public string RabbitMqUserConfig { get; set; } = "RabbitMq:User";

        public string RabbitMqPasswordConfig { get; set; } = "RabbitMq:Password";

        public string RabbitMqHostConfig { get; set; } = "RabbitMq:Host";

        // Serilog

        public string SerilogElasticEndpoint { get; set; } = "Serilog:ElasticEndpoint";

        public string SerilogIndexFormat { get; set; } = "Serilog:IndexFormat";

        public string SerilogElasticPassword { get; set; } = "Serilog:ElasticPassword";

        public string SerilogLogLevel { get; set; } = "Serilog:LogLevel";

        // JWT

        public string JwtSigningKeyConfig { get; set; } = "Jwt:SigningKey";

        public string JwtAudienceConfig { get; set; } = "Jwt:Audience";

        public string JwtIssuerConfig { get; set; } = "Jwt:Issuer";

        /// <summary>
        /// Adds a receive endpoint with the specified queue name. Use the consumer
        /// configurator action to specify which consumers should be connected to
        /// the specified queue.
        ///
        /// Calling this method will automatically set the "SetupMassTransit" property
        /// to true.
        /// </summary>
        public DefaultMicroserviceBuilder AddReceiveEndpoint(string queueName, Action<ConsumerCollection> consumerConfigurator)
        {
            SetupMassTransit = true;

            var endpoint = new ReceiveEndpointDefinition
            {
                Name = queueName,
                Consumers = new ConsumerCollection()
            };

            consumerConfigurator.Invoke(endpoint.Consumers);
            _receiveEndpointDefinitions.Add(endpoint);

            return this;
        }

        internal override void Build()
        {
            if (string.IsNullOrWhiteSpace(ServiceName))
                throw new ArgumentException("ServiceName must be specified!");

            if (ServiceVersion == null)
                throw new ArgumentException("ServiceVersion must be specified!");

            if (CorsAllowedHosts.Any() && !UseCors)
                throw new ArgumentException("Set 'UseCors' to true if you want to specify AllowedHosts");

            if (SetupMassTransit)
            {
                Services.SetupMassTransitServices()
                    .UseRabbitMqBusControl(rc =>
                    {

                        rc.SetLogging(Configuration[SerilogElasticEndpoint], Configuration[SerilogIndexFormat], Configuration[SerilogElasticPassword], Configuration[SerilogLogLevel]);

                        rc.Durable(true);
                        rc.SetCredentials(Configuration[RabbitMqUserConfig], Configuration[RabbitMqPasswordConfig]);
                        rc.SetHost(Configuration[RabbitMqHostConfig]);
                        rc.SetExchangeType(ExchangeType.Fanout);

                        if (SetupDefaultReceiveEndpoint)
                        {
                            rc.AddReceiveEndpoint(ServiceName, c =>
                            {
                                // Make copy
                                var assemblies = new[] { Assembly.GetEntryAssembly() };

                                c.AddConsumersInheritingFrom<IConsumer>(assemblies.ToArray());
                            });
                        }

                        foreach (var endpointDefinition in _receiveEndpointDefinitions)
                        {
                            rc.AddReceiveEndpoint(endpointDefinition);
                        }
                        rc.Durable(true);
                    });
            }

            if (UseDefaultSwaggerSetup)
            {
                Services.SetupSwaggerServices(sc =>
                {
                    sc.Version(ServiceVersion);
                    sc.IncludeXmlComments();
                    sc.Title(ServiceName);
                });
            }

            if (UseCors)
            {
                var corsBuilder = new CorsPolicyBuilder();
                corsBuilder.AllowAnyHeader();
                corsBuilder.AllowAnyMethod();
                corsBuilder.SetIsOriginAllowed(origin => CorsAllowedHosts.Contains(origin));
                corsBuilder.AllowCredentials();
                corsBuilder.SetPreflightMaxAge(TimeSpan.FromDays(1));

                Services.AddCors(options =>
                {
                    options.AddPolicy("HelperAllowedHosts", corsBuilder.Build());
                });
            }

            if (UseJwtAuthentication)
            {
                // Authentication
                Services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                    .AddJwtBearer(cfg =>
                    {
                        cfg.Audience = Configuration[JwtAudienceConfig];

                        cfg.TokenValidationParameters = new TokenValidationParameters
                        {
                            ValidateAudience = true,
                            ValidAudience = Configuration[JwtAudienceConfig],

                            ValidateIssuer = true,
                            ValidIssuer = Configuration[JwtIssuerConfig],

                            ClockSkew = TimeSpan.FromMinutes(2),

                            IssuerSigningKey =
                                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration[JwtSigningKeyConfig]))
                        };
                    });
            }
        }
    }
}
