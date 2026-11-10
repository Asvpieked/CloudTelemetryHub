using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Microsoft.EntityFrameworkCore;
using TelemetryApi.Data;
using TelemetryApi.Services;
using TelemetryApi.Workers;

var builder = WebApplication.CreateBuilder(args);

// ==========================================
// 1. Connection Strings & Cloud Storage Setup
// ==========================================
var dbConnectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("DefaultConnection string is not configured.");

//var blobConnectionString = builder.Configuration.GetConnectionString("BlobStorage")?? "UseDevelopmentStorage=true";

var blobConnectionString = builder.Configuration.GetConnectionString("BlobStorage")
    ?? "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1;";

//var queueConnectionString = builder.Configuration.GetConnectionString("BlobStorage")?? "UseDevelopmentStorage=true";

var queueConnectionString = builder.Configuration.GetConnectionString("QueueStorage")
    ?? "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;QueueEndpoint=http://127.0.0.1:10001/devstoreaccount1;";
/*
// Azure Blob Client (Single registration with pinned API version)
builder.Services.AddSingleton(x =>
{
    var options = new BlobClientOptions(BlobClientOptions.ServiceVersion.V2023_11_03)
    {
        Retry =
        {
            MaxRetries = 3,
            NetworkTimeout = TimeSpan.FromSeconds(10)
        }
    };
    return new BlobServiceClient(blobConnectionString, options);
});
builder.Services.AddScoped<IBlobStorageService, AzureBlobStorageService>();

// Azure Queue Client
builder.Services.AddSingleton(x =>
{
    var options = new QueueClientOptions(QueueClientOptions.ServiceVersion.V2023_11_03);
    return new QueueServiceClient(queueConnectionString, options);
});
builder.Services.AddScoped<IQueueService, AzureQueueService>();
builder.Services.AddHostedService<TelemetryAlertProcessor>();
*/

// Register Azure Blob Client
builder.Services.AddSingleton(x =>
{
    var options = new BlobClientOptions(BlobClientOptions.ServiceVersion.V2023_11_03)
    {
        Retry = { MaxRetries = 3, NetworkTimeout = TimeSpan.FromSeconds(10) }
    };
    return new BlobServiceClient(blobConnectionString, options);
});
builder.Services.AddScoped<IBlobStorageService, AzureBlobStorageService>();

// Register Azure Queue Client
builder.Services.AddSingleton(x =>
{
    var options = new QueueClientOptions(QueueClientOptions.ServiceVersion.V2023_11_03)
    {
        // Azure Queues require Base64 encoding for message body
        MessageEncoding = QueueMessageEncoding.Base64,
        Retry = { MaxRetries = 3, NetworkTimeout = TimeSpan.FromSeconds(10) }
    };
    return new QueueServiceClient(queueConnectionString, options);
});
builder.Services.AddScoped<IQueueService, AzureQueueService>();
builder.Services.AddHostedService<TelemetryAlertProcessor>();

builder.Services.Configure<HostOptions>(options =>
{
    options.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore;
});


// ==========================================
// 2. Database Context & Cloud Resilience
// ==========================================
builder.Services.AddDbContext<TelemetryDbContext>(options =>
{
    options.UseNpgsql(dbConnectionString, npgsqlOptions =>
    {
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorCodesToAdd: null);

        npgsqlOptions.CommandTimeout(30);
    });
});

// ==========================================
// 3. Health Checks & Core API Services
// ==========================================
builder.Services.AddHealthChecks()
    .AddDbContextCheck<TelemetryDbContext>(name: "database_health_check");

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// ==========================================
// 4. HTTP Request Pipeline
// ==========================================
if (app.Environment.IsDevelopment() || app.Environment.IsStaging())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();

app.MapHealthChecks("/healthz");
app.MapControllers();

// ==========================================
// 5. Automatic EF Core Schema Migration
// ==========================================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    var dbContext = services.GetRequiredService<TelemetryDbContext>();

    try
    {
        logger.LogInformation("Applying pending EF Core database migrations...");
        await dbContext.Database.MigrateAsync();
        logger.LogInformation("Database migrations applied successfully.");
    }
    catch (Exception ex)
    {
        logger.LogCritical(ex, "Failed to connect or migrate database.");
        throw;
    }
}

app.Run();