using MassTransit;

namespace TelemetryApi.Models
{
    public class TelemetryAlertMessage
    {
        //public Guid AlertId { get; set; } = Guid.NewGuid();
        public Guid AlertId { get; set; } = NewId.NextGuid();
        public string DeviceId { get; set; } = string.Empty;
        public double MetricValue { get; set; }
        public string MetricType { get; set; } = "Temperature";
        public string Severity { get; set; } = "High";
        public DateTime DetectedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
