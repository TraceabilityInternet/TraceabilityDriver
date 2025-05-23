using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Runtime.InteropServices;
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

            // conditionally setup MSC event profiles
            if ("true".Equals(configuration["EnableMSC"], StringComparison.OrdinalIgnoreCase))
            {
                OpenTraceability.MSC.Setup.Initialize();
            }
            else
            {
                OpenTraceability.GDST.Setup.Initialize();
            }

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
                    options.UseSqlServer(sqlServerConnectionString, options =>
                    {
                        options.EnableRetryOnFailure();
                    });
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
            services.AddTransient<TDMySqlConnector>();

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

            // Persist keys on the file system to support release installations as well as docker deployments
            string keysPath = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                            ? @"C:\home\keys"
                            : "/home/keys";
            services.AddDataProtection()
                .PersistKeysToFileSystem(new DirectoryInfo(keysPath))
                .SetApplicationName("TraceabilityDriver");

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
                            ValidIssuer = authConfig["Authority"], // Set the valid issuer
                            ValidateAudience = true,   // Ensure the audience matches your API
                            ValidAudience = authConfig["Audience"], // Set the valid audience
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

            // docker containers are often deployed behind a reverse proxy that handles https communication.
            // In those cases, the app in the container itself does not need to enforce https.
            // However, if someone needs to configure the app to use https, we want them to be able to do so.
            // Configuring the app to use https redirection would require them to mount the cert to the container in their docker compose file,
            // configure the app to expose an https port, and set the app to use https redirection here.
            //        -ASPNETCORE_URLS = https://+:443;http://+:80
            //        -ASPNETCORE_Kestrel__Certificates__Default__Password=<certificate-password>
            //        -ASPNETCORE_Kestrel__Certificates__Default__Path =/<path-to-your-certificate-file>/aspnetapp.pfx
            if (!"TRUE".Equals(Environment.GetEnvironmentVariable("DISABLE_HTTPS_REDIRECTION"), StringComparison.OrdinalIgnoreCase))
            {
                app.UseHttpsRedirection();
            }

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