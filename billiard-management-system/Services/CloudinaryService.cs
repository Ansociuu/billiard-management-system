using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace billiard_management_system.Services
{
    public interface ICloudinaryService
    {
        bool IsConfigured();
        (bool Valid, string Message) ValidateImageUrl(string url);
        Task<(bool Success, string ImageUrl, string Message)> UploadProductImageAsync(string localFilePath);
    }

    /// <summary>
    /// Dịch vụ tải ảnh sản phẩm lên Cloudinary qua REST API (không lưu secret cứng trong mã nguồn)
    /// Cấu hình đọc từ biến môi trường hoặc connections.config (đã được .gitignore)
    /// </summary>
    public class CloudinaryService : ICloudinaryService
    {
        private static readonly HttpClient Http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp", ".gif", ".bmp" };
        private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB

        private string _cloudName;
        private string _apiKey;
        private string _apiSecret;
        private string _uploadPreset;

        public CloudinaryService()
        {
            LoadConfiguration();
        }

        private void LoadConfiguration()
        {
            // 1. Ưu tiên đọc từ biến môi trường hệ thống
            _cloudName = Environment.GetEnvironmentVariable("CLOUDINARY_CLOUD_NAME");
            _apiKey = Environment.GetEnvironmentVariable("CLOUDINARY_API_KEY");
            _apiSecret = Environment.GetEnvironmentVariable("CLOUDINARY_API_SECRET");
            _uploadPreset = Environment.GetEnvironmentVariable("CLOUDINARY_UPLOAD_PRESET");

            // Hỗ trợ định dạng CLOUDINARY_URL=cloudinary://API_KEY:API_SECRET@CLOUD_NAME
            string cloudinaryUrl = Environment.GetEnvironmentVariable("CLOUDINARY_URL");
            if (!string.IsNullOrWhiteSpace(cloudinaryUrl))
            {
                ParseCloudinaryUrl(cloudinaryUrl);
            }

            // 2. Đọc từ connections.config (file cấu hình cục bộ đã được .gitignore)
            try
            {
                var connSetting = ConfigurationManager.ConnectionStrings["Cloudinary"]?.ConnectionString;
                if (!string.IsNullOrWhiteSpace(connSetting))
                {
                    if (connSetting.StartsWith("cloudinary://", StringComparison.OrdinalIgnoreCase))
                    {
                        ParseCloudinaryUrl(connSetting);
                    }
                    else
                    {
                        var pairs = connSetting.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var pair in pairs)
                        {
                            var kv = pair.Split(new[] { '=' }, 2);
                            if (kv.Length != 2) continue;

                            string k = kv[0].Trim();
                            string v = kv[1].Trim();
                            if (string.IsNullOrWhiteSpace(v) || v.StartsWith("YOUR_", StringComparison.OrdinalIgnoreCase))
                                continue;

                            if (k.Equals("CloudName", StringComparison.OrdinalIgnoreCase)) _cloudName = v;
                            else if (k.Equals("ApiKey", StringComparison.OrdinalIgnoreCase)) _apiKey = v;
                            else if (k.Equals("ApiSecret", StringComparison.OrdinalIgnoreCase)) _apiSecret = v;
                            else if (k.Equals("UploadPreset", StringComparison.OrdinalIgnoreCase)) _uploadPreset = v;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Cloudinary Config Load Error]: {ex.Message}");
            }
        }

        private void ParseCloudinaryUrl(string url)
        {
            // cloudinary://api_key:api_secret@cloud_name
            var match = Regex.Match(url.Trim(), @"^cloudinary://([^:]+):([^@]+)@(.+)$", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                _apiKey = match.Groups[1].Value;
                _apiSecret = match.Groups[2].Value;
                _cloudName = match.Groups[3].Value;
            }
        }

        public bool IsConfigured()
        {
            if (string.IsNullOrWhiteSpace(_cloudName)) return false;
            bool hasSignedAuth = !string.IsNullOrWhiteSpace(_apiKey) && !string.IsNullOrWhiteSpace(_apiSecret);
            bool hasUnsignedPreset = !string.IsNullOrWhiteSpace(_uploadPreset);
            return hasSignedAuth || hasUnsignedPreset;
        }

        public (bool Valid, string Message) ValidateImageUrl(string url)
        {
            // Cho phép để trống ảnh sản phẩm (Tiêu chí 5: CRUD vẫn hoạt động khi không có ảnh)
            if (string.IsNullOrWhiteSpace(url))
            {
                return (true, string.Empty);
            }

            string cleanUrl = url.Trim();

            if (Uri.TryCreate(cleanUrl, UriKind.Absolute, out Uri parsedUri))
            {
                if (parsedUri.Scheme == Uri.UriSchemeHttp || parsedUri.Scheme == Uri.UriSchemeHttps)
                {
                    return (true, string.Empty);
                }

                if (parsedUri.IsFile && File.Exists(parsedUri.LocalPath))
                {
                    return (true, string.Empty);
                }
            }

            if (File.Exists(cleanUrl))
            {
                return (true, string.Empty);
            }

            return (false, "Đường dẫn ảnh (ImageUrl) không hợp lệ! Vui lòng nhập URL hợp lệ (http/https) hoặc tải ảnh lên Cloudinary.");
        }

        public async Task<(bool Success, string ImageUrl, string Message)> UploadProductImageAsync(string localFilePath)
        {
            if (string.IsNullOrWhiteSpace(localFilePath) || !File.Exists(localFilePath))
            {
                return (false, null, "Không tìm thấy tệp ảnh được chọn trên máy tính.");
            }

            var fileInfo = new FileInfo(localFilePath);
            string ext = (fileInfo.Extension ?? string.Empty).ToLowerInvariant();
            if (!AllowedExtensions.Contains(ext))
            {
                return (false, null, $"Định dạng ảnh '{ext}' không được hỗ trợ. Chỉ chấp nhận: JPG, PNG, WEBP, GIF, BMP.");
            }

            if (fileInfo.Length <= 0 || fileInfo.Length > MaxFileSizeBytes)
            {
                return (false, null, "Kích thước tệp ảnh không hợp lệ (tối đa 10 MB).");
            }

            LoadConfiguration();

            if (!IsConfigured())
            {
                return (false, null,
                    "Chưa cấu hình tài khoản Cloudinary trong connections.config (hoặc biến môi trường CLOUDINARY_URL). " +
                    "Vui lòng bổ sung CloudName, ApiKey, ApiSecret vào connections.config.");
            }

            try
            {
                string endpoint = $"https://api.cloudinary.com/v1_1/{_cloudName}/image/upload";
                long timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                string folder = "billiard_products";

                using (var form = new MultipartFormDataContent())
                {
                    byte[] fileBytes = File.ReadAllBytes(localFilePath);
                    var fileContent = new ByteArrayContent(fileBytes);
                    form.Add(fileContent, "file", fileInfo.Name);

                    if (!string.IsNullOrWhiteSpace(_apiKey) && !string.IsNullOrWhiteSpace(_apiSecret))
                    {
                        // Signed upload
                        string paramToSign = $"folder={folder}&timestamp={timestamp}{_apiSecret}";
                        string signature = ComputeSha1Hex(paramToSign);

                        form.Add(new StringContent(folder), "folder");
                        form.Add(new StringContent(timestamp.ToString()), "timestamp");
                        form.Add(new StringContent(_apiKey), "api_key");
                        form.Add(new StringContent(signature), "signature");
                    }
                    else
                    {
                        // Unsigned upload với UploadPreset
                        form.Add(new StringContent(_uploadPreset), "upload_preset");
                        form.Add(new StringContent(folder), "folder");
                    }

                    var response = await Http.PostAsync(endpoint, form);
                    string responseJson = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        string secureUrl = ExtractJsonStringField(responseJson, "secure_url")
                                        ?? ExtractJsonStringField(responseJson, "url");

                        if (!string.IsNullOrWhiteSpace(secureUrl))
                        {
                            secureUrl = secureUrl.Replace("\\/", "/");
                            return (true, secureUrl, "Tải ảnh sản phẩm lên Cloudinary thành công!");
                        }
                    }

                    string errorDetail = ExtractJsonStringField(responseJson, "message") ?? $"HTTP {(int)response.StatusCode}";
                    return (false, null, $"Tải ảnh lên Cloudinary thất bại: {errorDetail}");
                }
            }
            catch (Exception ex)
            {
                return (false, null, $"Lỗi kết nối tới Cloudinary khi tải ảnh: {ex.Message}");
            }
        }

        private static string ComputeSha1Hex(string input)
        {
            using (var sha1 = SHA1.Create())
            {
                byte[] hashBytes = sha1.ComputeHash(Encoding.UTF8.GetBytes(input));
                var sb = new StringBuilder(hashBytes.Length * 2);
                foreach (byte b in hashBytes)
                {
                    sb.Append(b.ToString("x2"));
                }
                return sb.ToString();
            }
        }

        private static string ExtractJsonStringField(string json, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            var match = Regex.Match(json, $"\"{Regex.Escape(fieldName)}\"\\s*:\\s*\"([^\"]+)\"");
            return match.Success ? match.Groups[1].Value : null;
        }
    }
}
