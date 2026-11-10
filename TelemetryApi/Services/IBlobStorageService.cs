namespace TelemetryApi.Services;

public interface IBlobStorageService
{
    Task<string> UploadStreamAsync(string containerName, string blobName, Stream content, string contentType);
    Task<Stream?> DownloadStreamAsync(string containerName, string blobName);
    string GenerateSasUrl(string containerName, string blobName, TimeSpan validity);
}