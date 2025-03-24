using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using TraceabilityDriver;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
var config = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithProcessId()
    .Enrich.WithThreadId()
    .WriteTo.Console()
    .WriteTo.File("Logs/traceability_driver-.log",
        rollingInterval: RollingInterval.Day,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}");

string? mongoDbConnectionString = builder.Configuration["MongoDB:ConnectionString"];
if (!string.IsNullOrWhiteSpace(mongoDbConnectionString))
{
    config.WriteTo.MongoDB(mongoDbConnectionString, collectionName: "logs");
}

Log.Logger = config.CreateLogger();

builder.Host.UseSerilog(); // Use Serilog instead of default .NET logger

Startup startup = new Startup(builder.Configuration);
startup.ConfigureServices(builder.Services);

var app = builder.Build();

startup.Configure(app, app.Environment);

try
{
    Log.Information("Starting TraceabilityDriver application");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}