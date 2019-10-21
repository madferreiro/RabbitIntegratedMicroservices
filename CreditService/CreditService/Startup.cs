using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AutoMapper;
using Elasticsearch.Net;
using MassTransit;
using Microservices.Common;
using Microsoft.AspNet.OData.Extensions;
using Microsoft.AspNet.OData.Formatter;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;
using Nest;
using Serilog;
using CreditService.Repository;


namespace CreditService
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        private Dictionary<string, Func<LoggerConfiguration, LoggerConfiguration>> SetLogLevel { get; set; } = new Dictionary<string, Func<LoggerConfiguration, LoggerConfiguration>>()
        {
            { "Verbose", (lc) => { return lc.MinimumLevel.Verbose(); } },
            { "Debug", (lc) => { return lc.MinimumLevel.Debug(); } },
            { "Warning", (lc) => { return lc.MinimumLevel.Warning(); } },
            { "Error", (lc) => { return lc.MinimumLevel.Error(); } },
            { "Fatal", (lc) => { return lc.MinimumLevel.Fatal(); } }
        };
        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            //DbContext
            string connectionString = Configuration["Database:ConnectionString"];
            services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));


            //AutoMapper
            //services.AddAutoMapper(typeof(CoreMappingProfile).GetTypeInfo().Assembly);
            services.AddHttpClient();
            // DI
            //services.AddScoped<IAssortmentsManager, AssortmentsManager>();
            //services.AddTransient<IAffectedAssortmentsProviderFactory, AffectedAssortmentsProviderFactory>();


            services.AddMvc(op =>
            {
                foreach (var formatter in op.OutputFormatters.OfType<ODataOutputFormatter>()
                    .Where(it => !it.SupportedMediaTypes.Any()))
                {
                    formatter.SupportedMediaTypes.Add(
                        new MediaTypeHeaderValue("application/prs.mock-odata"));
                }

                foreach (var formatter in op.InputFormatters.OfType<ODataInputFormatter>()
                    .Where(it => !it.SupportedMediaTypes.Any()))
                {
                    formatter.SupportedMediaTypes.Add(
                        new MediaTypeHeaderValue("application/prs.mock-odata"));
                }
            }).SetCompatibilityVersion(CompatibilityVersion.Version_3_0);

            services.AddOData();

            // Workaround: https://github.com/OData/WebApi/issues/1177
            services.AddMvcCore(options =>
            {
                foreach (var outputFormatter in options.OutputFormatters.OfType<ODataOutputFormatter>().Where(_ => _.SupportedMediaTypes.Count == 0))
                {
                    outputFormatter.SupportedMediaTypes.Add(new MediaTypeHeaderValue("application/prs.odatatestxx-odata"));
                }
                foreach (var inputFormatter in options.InputFormatters.OfType<ODataInputFormatter>().Where(_ => _.SupportedMediaTypes.Count == 0))
                {
                    inputFormatter.SupportedMediaTypes.Add(new MediaTypeHeaderValue("application/prs.odatatestxx-odata"));
                }
            });
            //Setup Microservice using MicroservicesCommon
            services.SetupMicroservice(Configuration, c =>
            {
                c.ServiceName = "CreditService";
                c.ServiceVersion = new Version(1, 0);
                c.CorsAllowedHosts.Add("http://developer.mozilla.org"); // http example
                c.CorsAllowedHosts.Add("https://developer.mozilla.org");// https example
                c.AddReceiveEndpoint("creditservice",
                    cc =>
                    {
                        //add listeners here
                    });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
