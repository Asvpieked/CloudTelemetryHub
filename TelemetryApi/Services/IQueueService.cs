namespace TelemetryApi.Services
{
    public interface IQueueService
    {
        Task SendAlertAsync<T>(string queueName, T message);
    }
}
