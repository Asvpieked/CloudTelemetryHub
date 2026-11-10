using MassTransit;

namespace TelemetryApi.Models
{
    public class TelemetryRecord
    {
        //public Guid Id { get; set; } = Guid.NewGuid();

        public Guid Id { get; set; } = NewId.NextGuid();

        //private Guid _id;

        //public Guid Id
        //{
        //    get => _id == Guid.Empty ? (_id = Guid.NewGuid()) : _id;
        //    set => _id = value == Guid.Empty ? Guid.NewGuid() : value;
        //}
        public string DeviceId { get; set; } = string.Empty;
        public double Temperature { get; set; }
        public double Humidity { get; set; }
        public double Voltage { get; set; }
        public string Status { get; set; } = "Normal";
        public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
    }
}
