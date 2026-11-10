using Azure.Storage.Queues;
using System.Text.Json;
using TelemetryApi.Models;

namespace TelemetryApi.Workers
{
    public class TelemetryAlertProcessor : BackgroundService
    {
        private readonly QueueServiceClient _queueServiceClient;
        private readonly ILogger<TelemetryAlertProcessor> _logger;
        private const string QueueName = "telemetry-alerts";

        public TelemetryAlertProcessor(QueueServiceClient queueServiceClient, ILogger<TelemetryAlertProcessor> logger)
        {
            _queueServiceClient = queueServiceClient;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Telemetry Alert Queue Processor starting up...");

            var queueClient = _queueServiceClient.GetQueueClient(QueueName);
            try
            {
                await queueClient.CreateIfNotExistsAsync(cancellationToken: stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Could not connect to queue endpoint ({Message}). Background queue processor will idle.", ex.Message);
                return; // Exit worker cleanly without crashing the container
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Dequeue up to 5 messages with a 30-second invisibility timeout
                    var messages = await queueClient.ReceiveMessagesAsync(
                        maxMessages: 5,
                        visibilityTimeout: TimeSpan.FromSeconds(30),
                        cancellationToken: stoppingToken);

                    foreach (var msg in messages.Value)
                    {
                        var rawJson = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(msg.MessageText));
                        var alert = JsonSerializer.Deserialize<TelemetryAlertMessage>(rawJson);

                        if (alert != null)
                        {
                            // Simulate async alert processing (SMS, PagerDuty, Email dispatch)
                            _logger.LogWarning(
                                "[ALERT WORKER] Processing {Severity} alert for {DeviceId}: {MetricType} exceeded threshold ({Value}). AlertId: {AlertId}",
                                alert.Severity, alert.DeviceId, alert.MetricType, alert.MetricValue, alert.AlertId);
                        }

                        // Acknowledge and delete message once successfully processed
                        await queueClient.DeleteMessageAsync(msg.MessageId, msg.PopReceipt, stoppingToken);
                    }
                }
                catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
                {
                    _logger.LogError(ex, "Error occurred processing messages from queue {QueueName}", QueueName);
                }

                // Polling interval
                await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
            }
        }
    }
}
