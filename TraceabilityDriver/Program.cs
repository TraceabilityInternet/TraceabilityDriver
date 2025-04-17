using Serilog;
using Serilog.Sinks.MSSqlServer;
using System.Data;
using TraceabilityDriver;
using TraceabilityDriver.Services;

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
else if (!string.IsNullOrWhiteSpace(builder.Configuration["SqlServer:ConnectionString"]))
{
    ColumnOptions columnOptions = new();
    columnOptions.AdditionalColumns = new List<SqlColumn>
    {
        new SqlColumn("MachineName", SqlDbType.NVarChar, dataLength: 100),
        new SqlColumn("ProcessId", SqlDbType.Int),
        new SqlColumn("ThreadId", SqlDbType.Int)
    };

    config.WriteTo.MSSqlServer(
        connectionString: builder.Configuration["SqlServer:ConnectionString"],
        sinkOptions: new MSSqlServerSinkOptions
        {
            TableName = "Logs",
            AutoCreateSqlTable = true,
            BatchPostingLimit = 1,
        },
        columnOptions: columnOptions);
}

Log.Logger = config.CreateLogger();

builder.Host.UseSerilog(); // Use Serilog instead of default .NET logger

Startup startup = new Startup(builder.Configuration);
startup.ConfigureServices(builder.Services);

var app = builder.Build();

startup.Configure(app, app.Environment);

try
{
    Log.Information("Initializing GDST Cache database...");
    using (var scope = app.Services.CreateScope())
    {
        var dbService = scope.ServiceProvider.GetRequiredService<IDatabaseService>();
        await dbService.InitializeDatabase();
    }
    Log.Information("GDST Cache database initialized successfully.");

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