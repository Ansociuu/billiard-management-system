using System;
using System.IO;

namespace billiard_management_system.Helpers
{
    /// <summary>
    /// Lưu trữ các tùy chọn cá nhân của người dùng trên máy cục bộ (Ghi nhớ tên đăng nhập)
    /// </summary>
    public static class UserPreferences
    {
        private static readonly string ConfigDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BilliardManagementSystem");

        private static readonly string ConfigFilePath = Path.Combine(ConfigDir, "preferences.ini");

        public static string SavedUsername { get; set; } = string.Empty;
        public static bool RememberMe { get; set; } = true;

        static UserPreferences()
        {
            Load();
        }

        public static void Load()
        {
            try
            {
                if (File.Exists(ConfigFilePath))
                {
                    var lines = File.ReadAllLines(ConfigFilePath);
                    foreach (var line in lines)
                    {
                        var parts = line.Split(new[] { '=' }, 2);
                        if (parts.Length == 2)
                        {
                            var key = parts[0].Trim();
                            var val = parts[1].Trim();

                            if (key.Equals("SavedUsername", StringComparison.OrdinalIgnoreCase))
                            {
                                SavedUsername = val;
                            }
                            else if (key.Equals("RememberMe", StringComparison.OrdinalIgnoreCase))
                            {
                                if (bool.TryParse(val, out bool remember))
                                {
                                    RememberMe = remember;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UserPreferences Load Error]: {ex.Message}");
            }
        }

        public static void Save(string username, bool rememberMe)
        {
            try
            {
                RememberMe = rememberMe;
                SavedUsername = rememberMe ? username : string.Empty;

                if (!Directory.Exists(ConfigDir))
                {
                    Directory.CreateDirectory(ConfigDir);
                }

                var content = $"RememberMe={RememberMe}\r\nSavedUsername={SavedUsername}\r\n";
                File.WriteAllText(ConfigFilePath, content);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UserPreferences Save Error]: {ex.Message}");
            }
        }
    }
}

