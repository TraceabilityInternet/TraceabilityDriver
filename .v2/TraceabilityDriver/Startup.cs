using Microsoft.Extensions.DependencyInjection;
using TraceabilityDriver.Services;
using TraceabilityDriver.Services.Connectors;
using TraceabilityDriver.Services.Mapping;
using TraceabilityDriver.Services.Mapping.Functions;

namespace TraceabilityDriver
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            // SERVICES
            services.AddSingleton<MongoDBService>();

            // HOSTED SERVICES
            services.AddHostedService<SynchronizeService>();

            // CONNECTORS
            services.AddSingleton<TDConnectorFactory>();
            services.AddTransient<TDSqlServerConnector>();

            // MAPPING
            services.AddTransient<IEventsTableMappingService, EventsTableMappingService>();
            services.AddTransient<EventsConverterService>();
            services.AddTransient<IEventsMergerService, EventsMergeByIdService>();

            // MAPPING FUNCTIONS
            services.AddSingleton<MappingFunctionFactory>();
            services.AddSingleton<DictionaryMappingFunction>();
        }
    }
} 