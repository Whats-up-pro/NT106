using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
using Google;
using MessagingApp.Config;

namespace MessagingApp.Services;

public sealed class FirebaseStorageService
{
    private static FirebaseStorageService? _instance;
    public static FirebaseStorageService Instance => _instance ??= new FirebaseStorageService();

    private readonly StorageClient _client;
    private readonly string _projectId;

    private FirebaseStorageService()
    {
        FirebaseConfig.Initialize();

        var credentialsPath = FirebaseConfig.GetCredentialsPath();
        if (string.IsNullOrWhiteSpace(credentialsPath) || !File.Exists(credentialsPath))
        {
            throw new FileNotFoundException($"Firebase credentials file not found: {credentialsPath}");
        }

        _projectId = TryReadProjectIdFromServiceAccountJson(credentialsPath) ?? FirebaseConfig.ProjectId;
        var credential = GoogleCredential.FromFile(credentialsPath);
        _client = StorageClient.Create(credential);
    }

    public async Task<(bool success, string message, string bucket, string objectName)> UploadFileAsync(
        string localFilePath,
        string bucket,
        string objectName,
        string? contentType = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(localFilePath) || !File.Exists(localFilePath))
            {
                return (false, "Không tìm thấy file.", bucket, objectName);
            }

            if (string.IsNullOrWhiteSpace(bucket))
            {
                bucket = FirebaseConfig.StorageBucket;
            }

            if (string.IsNullOrWhiteSpace(objectName))
            {
                objectName = $"uploads/{Guid.NewGuid():N}/{Path.GetFileName(localFilePath)}";
            }

            contentType ??= GuessContentType(localFilePath);

            await using var fs = File.OpenRead(localFilePath);
            try
            {
                await _client.UploadObjectAsync(bucket, objectName, contentType, fs);
                return (true, "Uploaded", bucket, objectName);
            }
            catch (GoogleApiException gex) when (gex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
            {
                // Likely: bucket doesn't exist. Try to auto-discover a valid bucket in this project and retry once.
                var discovered = TryDiscoverBucket();
                if (string.IsNullOrWhiteSpace(discovered))
                {
                    return (false,
                        $"Lỗi upload (bucket='{bucket}', object='{objectName}'): Bucket không tồn tại. " +
                        "Bạn cần bật Firebase Storage trong Firebase Console (Build → Storage) hoặc set biến môi trường FIREBASE_STORAGE_BUCKET đúng với bucket thật.",
                        bucket,
                        objectName);
                }

                // Reset stream and retry
                fs.Position = 0;
                await _client.UploadObjectAsync(discovered, objectName, contentType, fs);
                return (true, $"Uploaded (bucket auto-detected: {discovered})", discovered, objectName);
            }
        }
        catch (Exception ex)
        {
            return (false, $"Lỗi upload (bucket='{bucket}', object='{objectName}'): {ex.Message}", bucket, objectName);
        }
    }

    public async Task<(bool success, string message)> DownloadToFileAsync(string bucket, string objectName, string destinationPath)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(bucket)) bucket = FirebaseConfig.StorageBucket;
            if (string.IsNullOrWhiteSpace(objectName)) return (false, "Thiếu đường dẫn file trên Storage.");
            if (string.IsNullOrWhiteSpace(destinationPath)) return (false, "Thiếu đường dẫn lưu file.");

            var dir = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrWhiteSpace(dir))
            {
                Directory.CreateDirectory(dir);
            }

            await using var fs = File.Open(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None);
            try
            {
                await _client.DownloadObjectAsync(bucket, objectName, fs);
                return (true, "Downloaded");
            }
            catch (GoogleApiException gex) when (gex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
            {
                var discovered = TryDiscoverBucket();
                if (string.IsNullOrWhiteSpace(discovered))
                {
                    return (false,
                        $"Không tìm thấy bucket '{bucket}'. Hãy bật Firebase Storage hoặc set FIREBASE_STORAGE_BUCKET đúng.");
                }

                fs.Position = 0;
                await _client.DownloadObjectAsync(discovered, objectName, fs);
                return (true, $"Downloaded (bucket auto-detected: {discovered})");
            }
        }
        catch (Exception ex)
        {
            return (false, $"Lỗi tải file: {ex.Message}");
        }
    }

    private string? TryDiscoverBucket()
    {
        try
        {
            var buckets = new List<string>();
            foreach (var b in _client.ListBuckets(_projectId))
            {
                if (!string.IsNullOrWhiteSpace(b.Name)) buckets.Add(b.Name);
            }

            if (buckets.Count == 0) return null;

            // Prefer Firebase default bucket patterns
            string? preferred = buckets.FirstOrDefault(x => x.EndsWith(".appspot.com", StringComparison.OrdinalIgnoreCase)
                                                           && x.Contains(_projectId, StringComparison.OrdinalIgnoreCase));
            preferred ??= buckets.FirstOrDefault(x => x.EndsWith(".appspot.com", StringComparison.OrdinalIgnoreCase));
            preferred ??= buckets.FirstOrDefault();
            return preferred;
        }
        catch
        {
            return null;
        }
    }

    private static string? TryReadProjectIdFromServiceAccountJson(string path)
    {
        try
        {
            var json = File.ReadAllText(path);
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("project_id", out var pid) && pid.ValueKind == JsonValueKind.String)
            {
                return pid.GetString();
            }
        }
        catch
        {
            // ignore
        }
        return null;
    }

    private static string GuessContentType(string filePath)
    {
        var ext = Path.GetExtension(filePath)?.ToLowerInvariant() ?? string.Empty;
        return ext switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".webp" => "image/webp",
            ".gif" => "image/gif",
            ".bmp" => "image/bmp",
            ".pdf" => "application/pdf",
            ".txt" => "text/plain",
            ".json" => "application/json",
            ".zip" => "application/zip",
            ".rar" => "application/vnd.rar",
            ".7z" => "application/x-7z-compressed",
            _ => "application/octet-stream"
        };
    }
}
