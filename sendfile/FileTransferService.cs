using Google.Cloud.Storage.V1;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using MessagingApp.Config;

namespace MessagingApp.Services
{
    /// <summary>
    /// Service for uploading files to Firebase Storage and returning public URL.
    /// </summary>
    public class FileTransferService
    {
        private static FileTransferService? _instance;
        public static FileTransferService Instance => _instance ??= new FileTransferService();

        private readonly StorageClient _storageClient;
        private readonly string _bucketName;

        private FileTransferService()
        {
            // Assumes GOOGLE_APPLICATION_CREDENTIALS is set and FirebaseConfig.ProjectId is valid
            _storageClient = StorageClient.Create();
            _bucketName = FirebaseConfig.ProjectId + ".appspot.com"; // Default Firebase Storage bucket pattern
        }

        /// <summary>
        /// Upload a local file to Firebase Storage under /uploads/{userId}/
        /// Returns success flag, message, publicUrl, storagePath.
        /// </summary>
        public async Task<(bool success, string message, string? publicUrl, string? storagePath)> UploadFileAsync(string userId, string localFilePath, string? contentType = null)
        {
            try
            {
                if (!File.Exists(localFilePath))
                {
                    return (false, "File không tồn tại", null, null);
                }

                string originalName = Path.GetFileName(localFilePath);
                string ext = Path.GetExtension(originalName);
                string safeExt = string.IsNullOrWhiteSpace(ext) ? "" : ext;
                string objectName = $"uploads/{userId}/{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid()}{safeExt}";

                // Basic content type inference if not provided
                if (string.IsNullOrWhiteSpace(contentType))
                {
                    contentType = InferContentType(ext);
                }

                using var fs = File.OpenRead(localFilePath);
                var obj = await _storageClient.UploadObjectAsync(_bucketName, objectName, contentType, fs);

                // Firebase Storage public URL (if not security restricted)
                string publicUrl = $"https://storage.googleapis.com/{_bucketName}/{objectName}";

                return (true, "Upload thành công", publicUrl, objectName);
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi upload: {ex.Message}", null, null);
            }
        }

        private string InferContentType(string ext)
        {
            ext = ext?.ToLowerInvariant() ?? "";
            return ext switch
            {
                ".png" => "image/png",
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".gif" => "image/gif",
                ".bmp" => "image/bmp",
                ".txt" => "text/plain",
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xls" => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".ppt" => "application/vnd.ms-powerpoint",
                ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                ".zip" => "application/zip",
                ".rar" => "application/vnd.rar",
                ".7z" => "application/x-7z-compressed",
                ".csv" => "text/csv",
                ".json" => "application/json",
                ".mp3" => "audio/mpeg",
                ".wav" => "audio/wav",
                ".mp4" => "video/mp4",
                ".mov" => "video/quicktime",
                _ => "application/octet-stream"
            };
        }
    }
}
