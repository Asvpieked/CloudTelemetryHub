using Azure.Storage.Queues;
using System.Text.Json;

namespace TelemetryApi.Services
{
    public class AzureQueueService : IQueueService
    {
        private readonly QueueServiceClient _queueServiceClient;
        private readonly ILogger<AzureQueueService> _logger;

        public AzureQueueService(QueueServiceClient queueServiceClient, ILogger<AzureQueueService> logger)
        {
            _queueServiceClient = queueServiceClient;
            _logger = logger;
        }

        public async Task SendAlertAsync<T>(string queueName, T message)
        {
            var queueClient = _queueServiceClient.GetQueueClient(queueName);
            await queueClient.CreateIfNotExistsAsync();

            var json = JsonSerializer.Serialize(message);

            // Base64 encode is required by Azure Queue Storage conventions
            var base64Message = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(json));

            await queueClient.SendMessageAsync(base64Message);
            _logger.LogInformation("Dispatched message to queue '{QueueName}'", queueName);
        }
    }
}
