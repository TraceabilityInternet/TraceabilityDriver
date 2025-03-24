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
            services.AddHostedService<ISynchronizeService, SynchronizeService>();

            // HANGFIRE
            //services.AddHangfire(configuration => configuration
            //    .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
            //    .UseSimpleAssemblyNameTypeSerializer()
            //    .UseRecommendedSerializerSettings()
            //    .UseMongoStorage(Configuration["MongoDB:ConnectionString"], new MongoStorageOptions
            //    {
            //        MigrationOptions = new MongoMigrationOptions()
            //        {
            //            MigrationStrategy = new MigrateMongoMigrationStrategy(),
            //            BackupStrategy = new CollectionMongoBackupStrategy()
            //        },
            //        Prefix = "hangfire.mongo",
            //        CheckConnection = true
            //    }));

            services.AddHangfireServer();

            // CONNECTORS
            services.AddSingleton<ITDConnectorFactory, TDConnectorFactory>();
            services.AddTransient<TDSqlServerConnector>();

            // MAPPING
            services.AddTransient<IEventsTableMappingService, EventsTableMappingService>();
            services.AddTransient<IEventsConverterService, EventsConverterService>();
            services.AddTransient<IEventsMergerService, EventsMergeByIdService>();

            // MAPPING FUNCTIONS
            services.AddSingleton<IMappingFunctionFactory, MappingFunctionFactory>();

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

            //app.UseHangfireDashboard();

            //// Schedule the synchronization job to run at midnight every day
            //RecurringJob.AddOrUpdate<ISynchronizeService>(
            //    "daily-synchronization",
            //    service => service.SynchronizeAsync(CancellationToken.None),
            //    Cron.Daily(0, 0));

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