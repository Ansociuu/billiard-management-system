using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using billiard_management_system.Helpers;
using billiard_management_system.Services;
using billiard_management_system.ViewModels;
using billiard_management_system.Views;

namespace billiard_management_system
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private readonly IAuditLogService _auditLogService = new AuditLogService();

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Đăng ký bộ bắt ngoại lệ toàn cục (Issue #25) để không bao giờ làm ứng dụng đóng đột ngột
            DispatcherUnhandledException += OnDispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

            // Chuyển sang OnExplicitShutdown để tránh việc WPF tự động tắt app 
            // khi MainWindow đóng lại để chuyển về LoginWindow
            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            if (e.Args.Contains("--test-db"))
            {
                var result = await DatabaseHelper.TestConnectionAsync();
                File.WriteAllText("db_test_result.txt", $"Success: {result.Success}\r\nMessage: {result.Message}\r\nTimestamp: {DateTime.Now}");
                Console.WriteLine($"DB Test: {result.Success} - {result.Message}");
                Shutdown(result.Success ? 0 : 1);
                return;
            }

            ShowLogin();
        }

        private async void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            e.Handled = true;
            string errMsg = e.Exception?.InnerException?.Message ?? e.Exception?.Message ?? "Lỗi không xác định trên luồng giao diện.";
            Debug.WriteLine($"[Global UI Exception Guard]: {e.Exception}");

            await _auditLogService.LogAsync(
                UserSession.CurrentUser?.UserId,
                "UNHANDLED_UI_ERROR",
                "System",
                null,
                $"Bắt gọn ngoại lệ giao diện không mong muốn: {errMsg}");

            MessageBox.Show(
                $"Hệ thống vừa ghi nhận một sự cố kỹ thuật nhưng đã bảo vệ an toàn phiên làm việc của bạn:\n\nChi tiết: {errMsg}\n\nVui lòng kiểm tra lại dữ liệu vừa nhập hoặc thử lại thao tác.",
                "Cảnh báo xử lý ngoại lệ an toàn",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }

        private void OnDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = e.ExceptionObject as Exception;
            Debug.WriteLine($"[Global AppDomain Exception Guard]: {ex}");
            _ = _auditLogService.LogAsync(
                UserSession.CurrentUser?.UserId,
                "UNHANDLED_DOMAIN_ERROR",
                "System",
                null,
                $"Ngoại lệ nền AppDomain: {ex?.Message}");
        }

        private void OnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            e.SetObserved();
            Debug.WriteLine($"[Global Task Exception Guard]: {e.Exception}");
            _ = _auditLogService.LogAsync(
                UserSession.CurrentUser?.UserId,
                "UNHANDLED_TASK_ERROR",
                "System",
                null,
                $"Ngoại lệ tác vụ bất đồng bộ (Task): {e.Exception?.GetBaseException()?.Message}");
        }

        private void ShowLogin()
        {
            var loginWindow = new LoginWindow();
            var loginVm = new LoginViewModel();

            // Nếu người dùng đóng cửa sổ Login bằng nút X đỏ mà chưa đăng nhập -> Tắt ứng dụng
            loginWindow.Closed += (s, e) =>
            {
                if (!UserSession.IsLoggedIn)
                {
                    Shutdown();
                }
            };

            loginVm.LoginSucceeded += () =>
            {
                var mainWindow = new MainWindow();
                var mainVm = new MainViewModel();

                mainVm.LogoutRequested += () =>
                {
                    // 1. Mở lại màn hình Login trước
                    ShowLogin();
                    // 2. Đóng màn hình chính MainWindow sau
                    mainWindow.Close();
                };

                // Nếu người dùng bấm nút X ở góc trên bên phải MainWindow -> Tắt ứng dụng
                mainWindow.Closed += (s, e) =>
                {
                    if (UserSession.IsLoggedIn)
                    {
                        Shutdown();
                    }
                };

                MainWindow = mainWindow;
                mainWindow.DataContext = mainVm;
                mainWindow.Show();

                loginWindow.Close();
            };

            MainWindow = loginWindow;
            loginWindow.DataContext = loginVm;
            loginWindow.Show();
        }
    }
}
