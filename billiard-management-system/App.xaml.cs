using System;
using System.IO;
using System.Linq;
using System.Windows;
using billiard_management_system.Helpers;
using billiard_management_system.ViewModels;
using billiard_management_system.Views;

namespace billiard_management_system
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

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
