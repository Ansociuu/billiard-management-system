using System;
using System.Configuration;
using System.Data;
using System.Threading.Tasks;
using MySqlConnector;

namespace billiard_management_system
{
    /// <summary>
    /// Lớp tiện ích quản lý kết nối cơ sở dữ liệu MySQL (Aiven Cloud)
    /// </summary>
    public static class DatabaseHelper
    {
        private static string _connectionString;

        public static string ConnectionString
        {
            get
            {
                if (string.IsNullOrEmpty(_connectionString))
                {
                    var config = ConfigurationManager.ConnectionStrings["BilliardDb"];
                    if (config != null && !string.IsNullOrWhiteSpace(config.ConnectionString))
                    {
                        _connectionString = config.ConnectionString;
                    }
                    else
                    {
                        throw new InvalidOperationException("Không tìm thấy chuỗi kết nối 'BilliardDb'. Vui lòng kiểm tra file connections.config (hoặc sao chép từ connections.config.example).");
                    }
                }
                return _connectionString;
            }
            set => _connectionString = value;
        }

        /// <summary>
        /// Tạo và mở một kết nối mới tới database
        /// </summary>
        public static MySqlConnection CreateConnection()
        {
            var conn = new MySqlConnection(ConnectionString);
            return conn;
        }

        /// <summary>
        /// Mở kết nối bất đồng bộ và đồng bộ múi giờ phiên MySQL theo giờ địa phương (UTC+7)
        /// để tránh lệch 7 tiếng khi MySQL Cloud chạy ở múi giờ UTC (+00:00).
        /// </summary>
        public static async Task<MySqlConnection> OpenConnectionAsync()
        {
            var conn = CreateConnection();
            await conn.OpenAsync();

            try
            {
                TimeSpan offset = TimeZoneInfo.Local.GetUtcOffset(DateTime.Now);
                string sign = offset >= TimeSpan.Zero ? "+" : "-";
                string tzString = $"{sign}{offset:hh\\:mm}";
                using (var tzCmd = new MySqlCommand($"SET time_zone = '{tzString}';", conn))
                {
                    await tzCmd.ExecuteNonQueryAsync();
                }
            }
            catch
            {
                // Bỏ qua nếu server hạn chế quyền SET time_zone; ứng dụng vẫn truyền trực tiếp DateTime.Now qua tham số SQL
            }

            return conn;
        }

        /// <summary>
        /// Kiểm tra kết nối tới cơ sở dữ liệu
        /// </summary>
        /// <returns>True nếu kết nối thành công, kèm thông điệp chi tiết</returns>
        public static async Task<(bool Success, string Message)> TestConnectionAsync()
        {
            try
            {
                using (var conn = await OpenConnectionAsync())
                {
                    using (var cmd = new MySqlCommand("SELECT COUNT(*) FROM Tables;", conn))
                    {
                        var count = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                        return (true, $"Kết nối cơ sở dữ liệu thành công! Tìm thấy {count} bàn chơi trong hệ thống.");
                    }
                }
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi kết nối cơ sở dữ liệu: {ex.Message}");
            }
        }
    }
}
