using Hangfire;
using Hangfire.Mongo;
using Hangfire.Mongo.Migration.Strategies;
using Hangfire.Mongo.Migration.Strategies.Backup;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using TraceabilityDriver.Models.Mapping;
using TraceabilityDriver.Pages;
using TraceabilityDriver.Services;
using TraceabilityDriver.Services.Connectors;
using TraceabilityDriver.Services.Mapping;
using TraceabilityDriver.Services.Mapping.Functions;

namespace TraceabilityDriver
{
    public class Startup
    {
        IConfiguration Configuration { get; set; } = null!;

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            // TRACEABILITY IDENTIFIER DOMAIN
            if (!string.IsNullOrWhiteSpace(Configuration["Traceability:IdentifierDomain"]))
            {
                CommonBaseModel.GDST_IDENTIFIERS_DOMAIN = Configuration["Traceability:IdentifierDomain"] ?? "example.org";
            }

            // ADD CONTROLLERS
            services.AddControllers();

            // SERVICES
            services.AddSingleton<IMongoDBService, MongoDBService>();
            services.AddSingleton<ISynchronizeService, SynchronizeService>();
            services.AddHostedService<HostedSyncService>();

            // CONNECTORS
            services.AddSingleton<ITDConnectorFactory, TDConnectorFactory>();
            services.AddTransient<TDSqlServerConnector>();

            // MAPPING
            services.AddScoped<IMappingContext, MappingContext>();
            services.AddTransient<IMappingSource, LocalMappingSource>();
            services.AddTransient<IEventsTableMappingService, EventsTableMappingService>();
            services.AddTransient<IEventsConverterService, EventsConverterService>();
            services.AddTransient<IEventsMergerService, EventsMergeByIdService>();

            // MAPPING FUNCTIONS
            services.AddSingleton<IMappingFunctionFactory, MappingFunctionFactory>();
            services.AddKeyedTransient<IMappingFunction, DictionaryMappingFunction>("dictionary");
            services.AddKeyedTransient<IMappingFunction, GenerateIdentifierFunction>("generateidentifier");
            services.AddKeyedTransient<IMappingFunction, JoinFunction>("join");

            // BLAZOR
            services.AddRazorComponents()
                    .AddInteractiveServerComponents();

            // ADD SWASH BUCKLER
            services.AddSwaggerGen();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // Configure the HTTP request pipeline.
            if (env.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseSerilogRequestLogging(); // Add HTTP request logging

            app.UseRouting();

            app.UseAuthentication();

            app.UseAuthorization();

            app.UseStaticFiles();
            app.UseAntiforgery();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers(); // For Web API
                endpoints.MapRazorComponents<App>()
                    .AddInteractiveServerRenderMode();
            });
        }
    }
} 