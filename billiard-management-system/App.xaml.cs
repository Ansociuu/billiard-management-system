using System;
using System.IO;
using System.Linq;
using System.Windows;
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

            loginVm.LoginSucceeded += () =>
            {
                var mainWindow = new MainWindow();
                var mainVm = new MainViewModel();

                mainVm.LogoutRequested += () =>
                {
                    mainWindow.Close();
                    ShowLogin();
                };

                mainWindow.DataContext = mainVm;
                mainWindow.Show();
                loginWindow.Close();
            };

            loginWindow.DataContext = loginVm;
            loginWindow.Show();
        }
    }
}
