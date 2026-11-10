using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TelemetryApi.Data;
using TelemetryApi.Services;

namespace TelemetryApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TelemetryReportsController : ControllerBase
{
    private readonly TelemetryDbContext _context;
    private readonly IBlobStorageService _storageService;
    private const string ContainerName = "telemetry-audit-logs";

    public TelemetryReportsController(TelemetryDbContext context, IBlobStorageService storageService)
    {
        _context = context;
        _storageService = storageService;
    }

    [HttpPost("export/{deviceId}")]
    public async Task<IActionResult> ExportTelemetry(string deviceId)
    {
        var records = await _context.TelemetryRecords
            .Where(r => r.DeviceId == deviceId)
            .OrderByDescending(r => r.TimestampUtc)
            .Take(500)
            .ToListAsync();

        if (records.Count == 0)
        {
            return NotFound($"No telemetry records found for device {deviceId}");
        }

        // Build CSV in-memory
        var csvBuilder = new StringBuilder();
        csvBuilder.AppendLine("Id,DeviceId,Temperature,Humidity,Voltage,Status,TimestampUtc");
        foreach (var item in records)
        {
            csvBuilder.AppendLine($"{item.Id},{item.DeviceId},{item.Temperature},{item.Humidity},{item.Voltage},{item.Status},{item.TimestampUtc:O}");
        }

        var fileName = $"{deviceId}/export_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvBuilder.ToString()));

        var blobUri = await _storageService.UploadStreamAsync(ContainerName, fileName, stream, "text/csv");

        // Generate a 1-hour secure pre-signed download URL
        string downloadUrl;
        try
        {
            downloadUrl = _storageService.GenerateSasUrl(ContainerName, fileName, TimeSpan.FromHours(1));
        }
        catch
        {
            downloadUrl = blobUri;
        }

        return Ok(new
        {
            Message = "Telemetry report exported successfully",
            BlobPath = fileName,
            DownloadUrl = downloadUrl
        });
    }

    [HttpGet("download")]
    public async Task<IActionResult> DownloadFile([FromQuery] string blobPath)
    {
        var stream = await _storageService.DownloadStreamAsync(ContainerName, blobPath);
        if (stream is null)
        {
            return NotFound("File not found in storage.");
        }

        return File(stream, "text/csv", Path.GetFileName(blobPath));
    }
}