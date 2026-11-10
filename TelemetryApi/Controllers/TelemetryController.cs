using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TelemetryApi.Data;
using TelemetryApi.Models;
using TelemetryApi.Services;

namespace TelemetryApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TelemetryController : ControllerBase
{
    private readonly TelemetryDbContext _context;
    private readonly ILogger<TelemetryController> _logger;

    public TelemetryController(TelemetryDbContext context, ILogger<TelemetryController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> IngestTelemetry(
        [FromBody] TelemetryRecord record,
        [FromServices] IQueueService queueService)
    {
        if (record.Id == Guid.Empty)
        {
            record.Id = Guid.NewGuid();
        }

        if (record.Temperature > 80.0)
        {
            record.Status = "Warning";
            _logger.LogWarning("High temperature alert for device {DeviceId}: {Temp}C", record.DeviceId, record.Temperature);

            // Push alert to cloud queue asynchronously
            await queueService.SendAlertAsync("telemetry-alerts", new TelemetryAlertMessage
            {
                DeviceId = record.DeviceId,
                MetricType = "Temperature",
                MetricValue = record.Temperature,
                Severity = record.Temperature > 90.0 ? "Critical" : "High"
            });
        }

        _context.TelemetryRecords.Add(record);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = record.Id }, record);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var record = await _context.TelemetryRecords.FindAsync(id);
        return record is null ? NotFound() : Ok(record);
    }

    [HttpGet("device/{deviceId}")]
    public async Task<IActionResult> GetByDevice(string deviceId, [FromQuery] int limit = 50)
    {
        var records = await _context.TelemetryRecords
            .Where(r => r.DeviceId == deviceId)
            .OrderByDescending(r => r.TimestampUtc)
            .Take(limit)
            .ToListAsync();

        return Ok(records);
    }
}