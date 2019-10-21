using System;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Swashbuckle.AspNetCore.Swagger;

namespace Microservices.Common.Swagger
{
    public class SwaggerServicesConfigurator : Builder
    {
        private IServiceCollection Services { get; }

        private Version _version = System.Version.Parse("1.0.0");

        private string _title = string.Empty;

        private Func<Version, string> _versionFormatter = v => $"v{v.Major}";

        private bool _includeXmlComments = false;

        public SwaggerServicesConfigurator(IServiceCollection services)
        {
            Services = services;
        }

        public SwaggerServicesConfigurator Version(Version version, Func<Version, string> versionFormatter = null)
        {
            _version = version;
            if (versionFormatter != null)
            {
                _versionFormatter = versionFormatter;
            }
            return this;
        }

        public SwaggerServicesConfigurator Title(string title)
        {
            _title = title;
            return this;
        }

        public SwaggerServicesConfigurator IncludeXmlComments(bool toggleXmlComments = true)
        {
            _includeXmlComments = toggleXmlComments;
            return this;
        }

        internal override void Build()
        {
            var versionStr = _versionFormatter.Invoke(_version);
            Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc(versionStr, new Info { Title = _title, Version = versionStr });
                if (_includeXmlComments)
                {
                    var xmlFile = $"{Assembly.GetEntryAssembly().GetName().Name}.xml";
                    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                    c.IncludeXmlComments(xmlPath);
                }
            });
        }
    }
}
