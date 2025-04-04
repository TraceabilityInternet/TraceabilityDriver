using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Serilog;
using TraceabilityDriver.Models.GDST;
using TraceabilityDriver.Models.Mapping;
using TraceabilityDriver.Pages;
using TraceabilityDriver.Services;
using TraceabilityDriver.Services.Authentication;
using TraceabilityDriver.Services.Connectors;
using TraceabilityDriver.Services.GDST;
using TraceabilityDriver.Services.Mapping;
using TraceabilityDriver.Services.Mapping.Functions;

namespace TraceabilityDriver
{
    public class Startup
    {
        IConfiguration Configuration { get; set; } = null!;

        public Startup(IConfiguration configuration)
        {
            OpenTraceability.Setup.Initialize();
            OpenTraceability.GDST.Setup.Initialize();

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
            services.AddHttpClient();

            // CONFIGURE DB
            string mongoDbConnectionString = Configuration["MongoDB:ConnectionString"] ?? string.Empty;
            string sqlServerConnectionString = Configuration["SqlServer:ConnectionString"] ?? string.Empty;
            if (!string.IsNullOrEmpty(mongoDbConnectionString))
            {
                services.AddSingleton<IDatabaseService, MongoDBService>();
            }
            else if(!string.IsNullOrEmpty(sqlServerConnectionString))
            {
                services.AddDbContextFactory<ApplicationDbContext>(options =>
                {
                    options.UseSqlServer(sqlServerConnectionString);
                });
                services.AddScoped<IDatabaseService, SqlServerService>();
            }
            else
            {
                throw new Exception("No database connection string found. Please set either MONGO_CONNECTION_STRING or SQL_CONNECTION_STRING.");
            }

            // SERVICES
            services.AddScoped<ISynchronizeService, SynchronizeService>();
            services.AddScoped<IGDSTCapabilityTestService, GDSTCapabilityTestService>();
            services.AddHostedService<HostedSyncService>();

            // OPTIONS
            services.Configure<GDSTCapabilityTestSettings>(Configuration.GetSection("GDST:CapabilityTest"));

            // CONNECTORS
            services.AddSingleton<ITDConnectorFactory, TDConnectorFactory>();
            services.AddTransient<TDSqlServerConnector>();

            // MAPPING
            services.AddSingleton<ISynchronizationContext, Services.SynchronizationContext>();
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

            // OAUTH (JWT) AUTHENTICATION
            var authenticationSchemeBuilder = services.AddAuthentication();
            List<string> policies = new();

            if (Configuration.GetSection("Authentication:JWT").Exists())
            {
                policies.Add("Bearer");

                // Configure JWT Bearer authentication
                services.AddAuthentication()
                    .AddJwtBearer("Bearer", options =>
                    {
                        // Bind settings from appsettings.json
                        var authConfig = Configuration.GetSection("Authentication:JWT");
                        options.Authority = authConfig["Authority"];
                        options.Audience = authConfig["Audience"];
                        options.MetadataAddress = authConfig["MetadataAddress"] ?? string.Empty;
                        options.RequireHttpsMetadata = bool.Parse(authConfig["RequireHttpsMetadata"] ?? "true");

                        // Optional: Additional validation rules
                        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                        {
                            ValidateIssuer = true,     // Ensure the issuer matches the Authority
                            ValidateAudience = true,   // Ensure the audience matches your API
                            ValidateLifetime = true,   // Check token expiration
                            ValidateIssuerSigningKey = true, // Validate the signature
                            ClockSkew = TimeSpan.FromMinutes(5) // Allow some clock skew
                        };
                    });
            }

            // API KEY AUTHENTICATION
            if (Configuration.GetSection("Authentication:APIKey").Exists())
            {
                services.AddSingleton<IApiKeyStore, InMemoryApiKeyStore>();

                policies.Add("ApiKey");

                authenticationSchemeBuilder.AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>("ApiKey", options =>
                {
                    var authConfig = Configuration.GetSection("Authentication:APIKey");
                    string headerName = authConfig["HeaderName"] ?? "X-API-Key";
                    options.HeaderName = headerName;
                });
            }

            if (!policies.Any())
            {
                policies.Add("AlwaysAuthenticated");
                authenticationSchemeBuilder.AddScheme<AuthenticationSchemeOptions, AlwaysAuthenticatedHandler>("AlwaysAuthenticated", null);
            }

            // Set default authentication scheme (optional)
            services.AddAuthorization(options =>
            {
                options.DefaultPolicy = new AuthorizationPolicyBuilder(policies.ToArray())
                    .RequireAuthenticatedUser()
                    .Build();
            });
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