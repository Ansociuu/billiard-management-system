using System;
using System.Security.Cryptography;
using System.Text;

namespace billiard_management_system.Helpers
{
    /// <summary>
    /// Các hàm tiện ích bảo mật mật khẩu và chuỗi dữ liệu
    /// </summary>
    public static class PasswordHelper
    {
        /// <summary>
        /// Tạo chuỗi hash SHA-256 cơ bản (kết hợp với BCrypt khi có package hỗ trợ)
        /// </summary>
        public static string ComputeSha256Hash(string rawData)
        {
            if (string.IsNullOrEmpty(rawData)) return string.Empty;

            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));
                var builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }
    }
}
